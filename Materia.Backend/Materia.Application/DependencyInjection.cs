using FluentValidation;
using Materia.Application.Commands.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace Materia.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<LoginCommandHandler>();
        services.AddValidatorsFromAssemblyContaining<LoginCommandHandler>();
        return services;
    }
}
