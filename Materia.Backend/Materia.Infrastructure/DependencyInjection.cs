using System.Text;
using Materia.Application.Contracts.Auth;
using Materia.Infrastructure.Auth;
using Materia.Infrastructure.Identity;
using Materia.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Materia.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options
                .UseNpgsql(configuration.GetConnectionString("materiadb"))
                // Snapshot is maintained manually — suppress the mismatch warning
                // that EF Core raises when the live model diverges from the snapshot.
                .ConfigureWarnings(w =>
                    w.Ignore(RelationalEventId.PendingModelChangesWarning)));

        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        var jwtSection = configuration.GetSection("Jwt");
        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSection["Issuer"],
                    ValidAudience = jwtSection["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtSection["Key"]!)),
                };
            });

        services.AddScoped<IUserAuthRepository, UserAuthRepository>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<DatabaseInitializer>();

        // Inventory repositories
        services.AddScoped<Application.Contracts.Inventory.IProductRepository, Inventory.ProductRepository>();
        services.AddScoped<Application.Contracts.Inventory.ICategoryRepository, Inventory.CategoryRepository>();
        services.AddScoped<Application.Contracts.Inventory.IProductQueryRepository, Inventory.ProductQueryRepository>();
        services.AddScoped<Application.Contracts.Inventory.ICategoryQueryRepository, Inventory.CategoryQueryRepository>();
        services.AddScoped<Application.Contracts.Inventory.IUnitRepository, Inventory.UnitRepository>();
        services.AddScoped<Application.Contracts.Inventory.IUnitQueryRepository, Inventory.UnitQueryRepository>();
        services.AddScoped<Application.Contracts.Inventory.IStockRepository, Inventory.StockRepository>();
        services.AddScoped<Application.Contracts.Inventory.IStockQueryRepository, Inventory.StockQueryRepository>();

        // Customer repositories
        services.AddScoped<Application.Contracts.Customers.ICustomerRepository, Customers.CustomerRepository>();
        services.AddScoped<Application.Contracts.Customers.ICustomerQueryRepository, Customers.CustomerQueryRepository>();

        // Purchasing repositories
        services.AddScoped<Application.Contracts.Purchasing.ISupplierRepository, Purchasing.SupplierRepository>();
        services.AddScoped<Application.Contracts.Purchasing.IPurchaseOrderRepository, Purchasing.PurchaseOrderRepository>();
        services.AddScoped<Application.Contracts.Purchasing.ISupplierQueryRepository, Purchasing.SupplierQueryRepository>();
        services.AddScoped<Application.Contracts.Purchasing.IPurchaseOrderQueryRepository, Purchasing.PurchaseOrderQueryRepository>();

        // Sales
        services.AddScoped<Application.Contracts.Sales.ISaleRepository, Sales.SaleRepository>();
        services.AddScoped<Application.Contracts.Sales.ISaleQueryRepository, Sales.SaleQueryRepository>();
        services.AddScoped<Application.Contracts.Sales.IStockDeductionService, Sales.StockDeductionService>();
        services.AddScoped<Application.Contracts.Sales.IReferenceNumberGenerator, Sales.ReferenceNumberGenerator>();

        return services;
    }
}
