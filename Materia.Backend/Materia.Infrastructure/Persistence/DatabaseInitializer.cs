using Materia.Infrastructure.Identity;
using Materia.Infrastructure.Stores;
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
        await SeedPlatformAdminAsync();
        await SeedUsersAsync();

        // The catalog (units / categories / products) belongs to the default store. The
        // seeder runs in a background scope with no HTTP request, so the current store is
        // supplied via an ambient StoreScope rather than the storeId JWT claim.
        using (StoreScope.Begin(StoreConstants.DefaultStoreId))
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
        var roles = SeedUsers.Select(u => u.Role).Append(StoreConstants.SuperAdminRole).Distinct();
        foreach (var role in roles)
        {
            if (await roleManager.RoleExistsAsync(role))
                continue;

            var result = await roleManager.CreateAsync(new IdentityRole(role));
            if (result.Succeeded)
                logger.LogInformation("Role '{Role}' created.", role);
            else
                logger.LogWarning("Failed to create role '{Role}': {Errors}", role,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    /// <summary>
    /// Seeds the platform <c>SuperAdmin</c> — a user with NO store (StoreId = null) that
    /// manages stores and provisions store-scoped users.
    /// </summary>
    private async Task SeedPlatformAdminAsync()
    {
        const string email = "superadmin@materia.local";
        if (await userManager.FindByEmailAsync(email) is not null)
            return;

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FullName = "Super Admin",
            EmailConfirmed = true,
            StoreId = null,
        };

        var result = await userManager.CreateAsync(user, "SuperAdmin@1234");
        if (!result.Succeeded)
        {
            logger.LogWarning("Failed to create platform admin: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
            return;
        }

        await userManager.AddToRoleAsync(user, StoreConstants.SuperAdminRole);
        logger.LogInformation("Platform admin '{Email}' created.", email);
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
                // The default store's own staff. SuperAdmin (seeded separately) has no store.
                StoreId = StoreConstants.DefaultStoreId,
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
