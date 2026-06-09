using System.Security.Claims;
using Materia.WebUi;
using Materia.WebUi.Components;
using Materia.WebUi.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// The container runs plain HTTP on :8080 behind a TLS-terminating reverse proxy
// (Caddy/Nginx) on the internal Docker network. Honor the proxy's X-Forwarded-*
// headers so the app sees the original https scheme and public host instead of the
// internal http://...:8080 hop. By default ForwardedHeaders only trusts loopback
// proxies, and a container-to-container hop is NOT loopback — so the headers would be
// dropped. Clearing the allowlists trusts the proxy. This is safe because :8080 is only
// reachable through the proxy on the private network, never published publicly.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddMudServices();
builder.Services.AddScoped<ThemeState>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Lets BearerTokenHandler reach the circuit-scoped AuthenticationStateProvider from the
// IHttpClientFactory handler scope, so the JWT is attached on interactive (SignalR) requests.
builder.Services.AddCircuitServicesAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();

var apiBaseUrl = builder.Configuration["ApiService:BaseUrl"] ?? "https://localhost:7072";

builder.Services.AddHttpClient<AuthService>(client =>
    client.BaseAddress = new Uri(apiBaseUrl));

builder.Services.AddTransient<BearerTokenHandler>();
builder.Services.AddHttpClient<InventoryApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<CustomerApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<SaleApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<SupplierApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<PurchaseOrderApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<FinancialApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

builder.Services.AddHttpClient<PettyCashApiClient>(client =>
    client.BaseAddress = new Uri(apiBaseUrl))
    .AddHttpMessageHandler<BearerTokenHandler>();

var app = builder.Build();

// MUST be the first middleware so everything downstream (HSTS, the auth-cookie Secure
// flag, generated redirects/absolute URLs, and the SignalR negotiate response) sees the
// real client scheme/host rather than the internal http://...:8080 hop.
app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);

// No UseHttpsRedirection(): TLS terminates at the reverse proxy and the container only
// listens on http://+:8080, so there is no https port to redirect to (Kestrel logs
// "Failed to determine the https port for redirect"). The proxy already upgrades
// http->https at the edge. Enabling it here only spams warnings and, if a port were ever
// inferred, risks a redirect loop on every request — including /_framework/blazor.web.js.

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(Materia.WebUi.Client._Imports).Assembly);

app.MapPost("/account/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).RequireAuthorization();

// Login form posts here — handles HttpContext.SignInAsync which requires a real HTTP request.
// DisableAntiforgery: token consistency is tricky across pre-render / circuit boundary;
// login CSRF risk is acceptable for an internal PoS app.
app.MapPost("/account/login-handler", async (
    HttpContext ctx,
    IFormCollection form,
    AuthService auth) =>
{
    var email     = form["email"].ToString();
    var password  = form["password"].ToString();
    var returnUrl = form["returnUrl"].ToString();

    var result = await auth.LoginAsync(email, password);
    if (!result.Succeeded)
    {
        var msg = Uri.EscapeDataString(result.ErrorMessage ?? "Email atau kata sandi salah.");
        return Results.Redirect($"/login?error={msg}");
    }

    var data = result.Data!;
    var claims = new List<Claim>
    {
        new(ClaimTypes.Email, data.Email),
        new(ClaimTypes.Name, data.FullName ?? data.Email),
        new("access_token", data.Token),
    };
    claims.AddRange(data.Roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);
    await ctx.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });

    // Validate returnUrl is a local relative path before redirecting
    if (!string.IsNullOrWhiteSpace(returnUrl)
        && returnUrl.StartsWith('/')
        && !returnUrl.StartsWith("//")
        && Uri.IsWellFormedUriString(returnUrl, UriKind.Relative))
        return Results.Redirect(returnUrl);

    return Results.Redirect("/");
}).DisableAntiforgery();

// Called by BearerTokenHandler when the API returns 401 (token expired / invalid).
// Signs out the cookie so the user gets a clean login form with an expiry notice.
app.MapGet("/account/session-expired", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login?expired=1");
});

app.Run();
