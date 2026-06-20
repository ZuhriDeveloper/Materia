using System.Threading.RateLimiting;

namespace Materia.WebApi.RateLimiting;

/// <summary>
/// Per-IP rate limiting for the authentication endpoints. Adds a second line of defence on top of
/// Identity's account lockout: throttles login brute-forcing, password-reset email spam, and
/// confirmation/token guessing — none of which lockout alone prevents.
/// </summary>
public static class AuthRateLimiting
{
    /// <summary>General per-IP throttle: login, reset-password, confirm-email, change-password.</summary>
    public const string GeneralPolicy = "auth";

    /// <summary>Stricter per-IP throttle for the email-sending forgot-password action.</summary>
    public const string EmailPolicy = "auth-email";

    // Requests allowed per client IP per window. Tuned to stop abuse while leaving headroom for a
    // real user retrying. forgot-password is stricter because each request can dispatch an email.
    private const int GeneralPermitLimit = 10;
    private const int EmailPermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(GeneralPolicy, httpContext => PerIp(httpContext, GeneralPermitLimit));
            options.AddPolicy(EmailPolicy, httpContext => PerIp(httpContext, EmailPermitLimit));

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();

                await context.HttpContext.Response.WriteAsJsonAsync(
                    new { message = "Terlalu banyak permintaan. Silakan coba lagi sebentar." }, token);
            };
        });

        return services;
    }

    // Partition by client IP so one abusive caller cannot exhaust the limit for everyone. Unknown
    // remote IPs share a single fallback partition.
    private static RateLimitPartition<string> PerIp(HttpContext httpContext, int permitLimit) =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = Window
            });
}
