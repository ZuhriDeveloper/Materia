using System.Net;
using FluentAssertions;
using Materia.WebApi.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Materia.Tests.Auth;

/// <summary>
/// Exercises the real <see cref="AuthRateLimiting"/> configuration over an in-memory test server,
/// so the limits and the 429 response are guarded against regressions without needing a database.
/// </summary>
public class AuthRateLimitingTests
{
    private static async Task<HttpClient> StartServerAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthRateLimiting();

        var app = builder.Build();
        app.UseRateLimiter();
        app.MapGet("/general", () => Results.Ok())
           .RequireRateLimiting(AuthRateLimiting.GeneralPolicy);
        app.MapGet("/email", () => Results.Ok())
           .RequireRateLimiting(AuthRateLimiting.EmailPolicy);

        await app.StartAsync();
        return app.GetTestClient();
    }

    [Fact]
    public async Task General_policy_allows_the_first_ten_requests_then_returns_429()
    {
        var client = await StartServerAsync();

        for (var i = 0; i < 10; i++)
            (await client.GetAsync("/general")).StatusCode.Should().Be(HttpStatusCode.OK);

        var rejected = await client.GetAsync("/general");

        rejected.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await rejected.Content.ReadAsStringAsync()).Should().Contain("Terlalu banyak permintaan");
    }

    [Fact]
    public async Task Email_policy_is_stricter_and_rejects_after_five_requests()
    {
        var client = await StartServerAsync();

        for (var i = 0; i < 5; i++)
            (await client.GetAsync("/email")).StatusCode.Should().Be(HttpStatusCode.OK);

        (await client.GetAsync("/email")).StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
