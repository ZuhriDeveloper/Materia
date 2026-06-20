using FluentValidation;
using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;
using Materia.WebApi.RateLimiting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Materia.WebApi.Controllers.Platform;

/// <summary>
/// Platform user management. Only a SuperAdmin can create store-scoped staff accounts (the store
/// they belong to is fixed at creation and flows into the JWT as the storeId claim), list them,
/// change their roles, or force a password reset.
/// </summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/platform/users")]
public class UsersController(
    IUserProvisioningService provisioning,
    IUserAdminService userAdmin,
    UpdateUserRolesCommandHandler updateRolesHandler,
    IValidator<UpdateUserRolesCommand> updateRolesValidator,
    AdminResetPasswordCommandHandler resetPasswordHandler) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ProvisionUserRequest request, CancellationToken ct)
    {
        var result = await provisioning.ProvisionAsync(
            request.Email, request.FullName, request.Password, request.Role, request.StoreId, ct);

        return result.Succeeded
            ? Ok(new { id = result.UserId })
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>Lists store-scoped users, optionally filtered to a single store.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? storeId, CancellationToken ct)
        => Ok(await userAdmin.ListUsersAsync(storeId, ct));

    /// <summary>Replaces a user's full role set (store roles only — not SuperAdmin).</summary>
    [HttpPut("{id}/roles")]
    public async Task<IActionResult> UpdateRoles(
        string id, [FromBody] UpdateUserRolesRequest request, CancellationToken ct)
    {
        var command = new UpdateUserRolesCommand(id, request.Roles ?? []);

        var validation = await updateRolesValidator.ValidateAsync(command, ct);
        if (!validation.IsValid)
            return BadRequest(new { errors = validation.Errors.Select(e => e.ErrorMessage) });

        var result = await updateRolesHandler.HandleAsync(command, ct);
        return result.Succeeded
            ? Ok(new { message = "Role pengguna berhasil diperbarui." })
            : BadRequest(new { errors = result.Errors });
    }

    /// <summary>
    /// Forces a password reset: generates a temporary password and emails it to the user.
    /// Rate-limited because it sends email.
    /// </summary>
    [EnableRateLimiting(AuthRateLimiting.EmailPolicy)]
    [HttpPost("{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, CancellationToken ct)
    {
        var result = await resetPasswordHandler.HandleAsync(new AdminResetPasswordCommand(id), ct);
        return result.Succeeded
            ? Ok(new { message = "Email berisi kata sandi sementara telah dikirim ke pengguna." })
            : BadRequest(new { errors = result.Errors });
    }
}

public record ProvisionUserRequest(
    string Email,
    string FullName,
    string Password,
    string Role,
    Guid StoreId);

public record UpdateUserRolesRequest(IReadOnlyList<string>? Roles);
