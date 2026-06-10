using Materia.Application.Contracts.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Materia.WebApi.Controllers.Platform;

/// <summary>
/// Platform user provisioning. Only a SuperAdmin can create store-scoped staff accounts
/// (the store they belong to is fixed at creation and flows into the JWT as the storeId
/// claim).
/// </summary>
[ApiController]
[Authorize(Roles = "SuperAdmin")]
[Route("api/platform/users")]
public class UsersController(IUserProvisioningService provisioning) : ControllerBase
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
}

public record ProvisionUserRequest(
    string Email,
    string FullName,
    string Password,
    string Role,
    Guid StoreId);
