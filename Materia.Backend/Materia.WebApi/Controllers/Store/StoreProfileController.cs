using System.Security.Claims;
using FluentValidation;
using Materia.Application.Commands.Stores.UpdateStoreParameters;
using Materia.Application.Contracts.Stores;
using Materia.Application.Queries.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Store;

/// <summary>
/// Store profile management — editable by the store's own Admin.
/// All endpoints are scoped to the current store via the <c>storeId</c> JWT claim.
/// </summary>
// NOTE: stacked [Authorize] attributes combine with AND semantics — the class level
// stays role-less and every action declares its own roles, otherwise a class-level
// "Admin" would override the Cashier access on the GET endpoints.
[ApiController]
[Authorize]
[Route("api/store/profile")]
public class StoreProfileController(
    GetMyStoreQueryHandler queryHandler,
    UpdateStoreParametersCommandHandler updateHandler,
    IStoreLogoRepository logoRepository,
    IValidator<UpdateStoreParametersCommand> updateValidator) : ControllerBase
{
    private string CurrentUser =>
        User.FindFirstValue("fullName") is { Length: > 0 } fn ? fn :
        User.FindFirstValue(ClaimTypes.Email) ??
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "unknown";

    // ── GET api/store/profile ─────────────────────────────────────────────────

    [HttpGet]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var profile = await queryHandler.HandleAsync(new GetMyStoreQuery(), ct);
        return Ok(profile);
    }

    // ── PUT api/store/profile ─────────────────────────────────────────────────

    [HttpPut]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateProfile(
        [FromBody] UpdateStoreProfileRequest request,
        CancellationToken ct)
    {
        var command = new UpdateStoreParametersCommand(
            request.Address,
            request.Phone,
            request.MaxDeliveryDistanceKm,
            CurrentUser);

        var validation = await updateValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        await updateHandler.HandleAsync(command, ct);
        return NoContent();
    }

    // ── POST api/store/profile/logo ───────────────────────────────────────────

    [HttpPost("logo")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(2 * 1024 * 1024)]
    public async Task<IActionResult> UploadLogo(IFormFile file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file was provided." } });

        // Validate content type declaration
        var allowedTypes = new[] { "image/jpeg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType, StringComparer.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Only JPEG, PNG, and WebP images are allowed." } });

        // Validate by magic bytes
        using var stream = file.OpenReadStream();
        var header = new byte[12];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);
        if (bytesRead < 4)
            return BadRequest(new { errors = new[] { "File content is too short or invalid." } });

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng  = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        // WebP: RIFF????WEBP
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                  && bytesRead >= 12
                  && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        string? detectedContentType = isJpeg ? "image/jpeg" : isPng ? "image/png" : isWebp ? "image/webp" : null;
        if (detectedContentType is null)
            return BadRequest(new { errors = new[] { "File content is not a supported image format." } });

        if (!string.Equals(file.ContentType, detectedContentType, StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Declared content type does not match file content." } });

        // Read remaining bytes
        stream.Position = 0;
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct);
        var content = ms.ToArray();

        var extension = detectedContentType switch
        {
            "image/jpeg" => ".jpg",
            "image/png"  => ".png",
            _            => ".webp"
        };
        var safeFileName = $"{Guid.NewGuid()}{extension}";

        // Resolve storeId from the JWT claim (CurrentStore pulls from HttpContext)
        var storeIdClaim = User.FindFirstValue("storeId");
        if (!Guid.TryParse(storeIdClaim, out var storeId))
            return Unauthorized();

        await logoRepository.SaveAsync(storeId, content, detectedContentType, safeFileName, CurrentUser, ct);
        return Ok(new { fileName = safeFileName });
    }

    // ── GET api/store/profile/logo ────────────────────────────────────────────

    [HttpGet("logo")]
    [Authorize(Roles = "Admin,Cashier")]
    public async Task<IActionResult> GetLogo(CancellationToken ct)
    {
        var storeIdClaim = User.FindFirstValue("storeId");
        if (!Guid.TryParse(storeIdClaim, out var storeId))
            return Unauthorized();

        var logo = await logoRepository.GetByStoreIdAsync(storeId, ct);
        if (logo is null)
            return NotFound();

        return File(logo.Content, logo.ContentType, logo.FileName);
    }
}

public record UpdateStoreProfileRequest(
    string Address,
    string Phone,
    decimal MaxDeliveryDistanceKm);
