using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Materia.WebUi.Services;

/// <summary>
/// Attaches the JWT access token to every outgoing API request.
/// Works in both SSR (reads from HttpContext) and Interactive Server
/// (reads from AuthenticationStateProvider, since HttpContext is null
/// after the SignalR handshake).
/// On <see cref="HttpStatusCode.Unauthorized"/>: navigates to the
/// /account/session-expired endpoint which signs out the cookie and
/// redirects to the login page with an expiry notice.
/// </summary>
public class BearerTokenHandler(
    IHttpContextAccessor accessor,
    IServiceProvider serviceProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken ct)
    {
        // 1. SSR path — HttpContext is available on the initial request
        var token = accessor.HttpContext?.User.FindFirstValue("access_token");

        // 2. Interactive Server path — HttpContext is null after SignalR handshake
        if (string.IsNullOrEmpty(token))
        {
            var authProvider = serviceProvider.GetService<AuthenticationStateProvider>();
            if (authProvider is not null)
            {
                var state = await authProvider.GetAuthenticationStateAsync();
                token = state.User.FindFirstValue("access_token");
            }
        }

        if (!string.IsNullOrEmpty(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, ct);

        // JWT expired or invalid — force the user back to the login page.
        // In SSR this throws NavigationException (caught by Blazor's renderer
        // which issues a proper HTTP redirect). In Interactive Server it sends
        // a navigation command to the browser over SignalR.
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var nav = serviceProvider.GetService<NavigationManager>();
            nav?.NavigateTo("/account/session-expired", forceLoad: true);
        }

        return response;
    }
}
