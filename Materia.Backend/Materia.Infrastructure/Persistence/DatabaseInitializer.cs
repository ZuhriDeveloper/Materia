using Materia.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Materia.Infrastructure.Persistence;

public class DatabaseInitializer(
    AppDbContext dbContext,
    RoleManager<IdentityRole> roleManager,
    UserManager<ApplicationUser> userManager,
    CatalogSeeder catalogSeeder,
    ILogger<DatabaseInitializer> logger)
{
    private static readonly SeedUser[] SeedUsers =
    [
        new("admin@materia.local",    "Admin",    "Admin Materia",   "Admin@1234"),
        new("cashier@materia.local",  "Cashier",  "Cashier Materia", "Cashier@1234"),
        new("helper@materia.local",   "Helper",   "Helper Materia",  "Helper@1234"),
    ];

    public async Task InitialiseAsync(CancellationToken cancellationToken = default)
    {
        await MigrateAsync(cancellationToken);
        await SeedRolesAsync();
        await SeedUsersAsync();
        await catalogSeeder.SeedAsync(cancellationToken);
    }

    private async Task MigrateAsync(CancellationToken cancellationToken)
    {
        var pending = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
        if (!pending.Any())
        {
            logger.LogInformation("No pending migrations.");
            return;
        }

        logger.LogInformation("Applying {Count} pending migration(s)…", pending.Count());
        await dbContext.Database.MigrateAsync(cancellationToken);
        logger.LogInformation("Migrations applied successfully.");
    }

    private async Task SeedRolesAsync()
    {
        foreach (var seed in SeedUsers)
        {
            if (await roleManager.RoleExistsAsync(seed.Role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(seed.Role));
            if (result.Succeeded)
                logger.LogInformation("Role '{Role}' created.", seed.Role);
            else
                logger.LogWarning("Failed to create role '{Role}': {Errors}", seed.Role,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedUsersAsync()
    {
        foreach (var seed in SeedUsers)
        {
            if (await userManager.FindByEmailAsync(seed.Email) is not null)
                continue;

            var user = new ApplicationUser
            {
                UserName = seed.Email,
                Email = seed.Email,
                FullName = seed.FullName,
                EmailConfirmed = true,
            };

            var result = await userManager.CreateAsync(user, seed.Password);
            if (!result.Succeeded)
            {
                logger.LogWarning("Failed to create user '{Email}': {Errors}", seed.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(user, seed.Role);
            logger.LogInformation("User '{Email}' created and assigned to role '{Role}'.", seed.Email, seed.Role);
        }
    }

    private record SeedUser(string Email, string Role, string FullName, string Password);
}
