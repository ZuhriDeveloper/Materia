using Microsoft.AspNetCore.Components.Server.Circuits;

namespace Materia.WebUi.Services;

/// <summary>
/// Exposes the current Blazor circuit's scoped <see cref="IServiceProvider"/> to code that runs
/// outside the circuit's DI scope.
/// <para>
/// <see cref="BearerTokenHandler"/> is created inside <c>IHttpClientFactory</c>'s long-lived
/// message-handler scope, NOT the per-circuit scope. Resolving a scoped service such as
/// <c>AuthenticationStateProvider</c> from the handler scope therefore yields a fresh, anonymous
/// instance with no authenticated user — so the JWT never gets attached during interactive
/// (SignalR) requests. This accessor publishes the circuit's own provider via an
/// <see cref="AsyncLocal{T}"/> that is set on every inbound circuit activity, letting the handler
/// resolve the authenticated provider instead.
/// </para>
/// Pattern from the official docs: "Access server-side Blazor services from a different DI scope."
/// </summary>
public sealed class CircuitServicesAccessor
{
    private static readonly AsyncLocal<IServiceProvider?> Current = new();

    public IServiceProvider? Services
    {
        get => Current.Value;
        set => Current.Value = value;
    }
}

/// <summary>
/// Publishes the circuit's scoped <see cref="IServiceProvider"/> into
/// <see cref="CircuitServicesAccessor"/> for the duration of each inbound circuit activity.
/// </summary>
internal sealed class ServicesAccessorCircuitHandler(
    IServiceProvider services,
    CircuitServicesAccessor accessor) : CircuitHandler
{
    public override Func<CircuitInboundActivityContext, Task> CreateInboundActivityHandler(
        Func<CircuitInboundActivityContext, Task> next)
        => async context =>
        {
            accessor.Services = services;
            await next(context);
        };
}

public static class CircuitServicesAccessorExtensions
{
    public static IServiceCollection AddCircuitServicesAccessor(this IServiceCollection services)
    {
        services.AddScoped<CircuitServicesAccessor>();
        services.AddScoped<CircuitHandler, ServicesAccessorCircuitHandler>();
        return services;
    }
}
