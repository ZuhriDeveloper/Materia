using System.Text;
using Materia.Application.Contracts.Auth;
using Materia.Infrastructure.Auth;
using Materia.Infrastructure.Identity;
using Materia.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            options.UseNpgsql(configuration.GetConnectionString("materiadb")));

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

        return services;
    }
}
