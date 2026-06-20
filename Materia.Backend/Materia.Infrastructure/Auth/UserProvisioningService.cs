using Materia.Application.Commands.Auth;
using Materia.Application.Contracts.Auth;
using Materia.Application.Contracts.Email;
using Materia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Materia.Infrastructure.Auth;

public class UserProvisioningService(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IAccountLinkBuilder linkBuilder,
    IEmailSender emailSender,
    ILogger<UserProvisioningService> logger) : IUserProvisioningService
{
    public async Task<UserProvisioningResult> ProvisionAsync(
        string email,
        string fullName,
        string password,
        string role,
        Guid? storeId,
        CancellationToken ct = default)
    {
        if (!await roleManager.RoleExistsAsync(role))
            return new UserProvisioningResult(false, null, [$"Role '{role}' does not exist."]);

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = fullName,
            // The address is unverified until the user clicks the confirmation link.
            // Login is intentionally NOT gated on confirmation (staff get credentials handed
            // to them); the email just verifies the address is reachable for password resets.
            EmailConfirmed = false,
            StoreId = storeId,
        };

        var create = await userManager.CreateAsync(user, password);
        if (!create.Succeeded)
            return new UserProvisioningResult(false, null,
                create.Errors.Select(e => e.Description).ToList());

        var addRole = await userManager.AddToRoleAsync(user, role);
        if (!addRole.Succeeded)
            return new UserProvisioningResult(false, user.Id,
                addRole.Errors.Select(e => e.Description).ToList());

        await SendConfirmationEmailAsync(user);

        return new UserProvisioningResult(true, user.Id, []);
    }

    /// <summary>
    /// Best-effort: a failure to send the confirmation email must not fail provisioning
    /// (the account is already created and login is not gated on confirmation).
    /// </summary>
    private async Task SendConfirmationEmailAsync(ApplicationUser user)
    {
        try
        {
            var token = IdentityTokenCodec.Encode(await userManager.GenerateEmailConfirmationTokenAsync(user));
            var link = linkBuilder.BuildEmailConfirmationLink(user.Id, token);
            var (subject, body) = AccountEmailTemplates.EmailConfirmation(user.FullName, link);
            await emailSender.SendAsync(user.Email!, user.FullName, subject, body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send confirmation email to {Email}", user.Email);
        }
    }
}
