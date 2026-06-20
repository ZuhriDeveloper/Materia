using Materia.Application;
using Materia.Domain.Common;
using Materia.Infrastructure;
using Materia.Infrastructure.Persistence;
using Materia.WebApi.RateLimiting;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Allow up to 2 MB for store logo uploads (Kestrel + form-body limits must both be set)
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = 2 * 1024 * 1024);
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 2 * 1024 * 1024;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddAuthRateLimiting();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration["WebUi:BaseUrl"] ?? "https://localhost:7266")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();
    await initializer.InitialiseAsync();
}

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Return domain rule violations as 400 Bad Request instead of 500
app.Use(async (ctx, next) =>
{
    try { await next(ctx); }
    catch (DomainException ex)
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { errors = new[] { ex.Message } });
    }
});

app.MapControllers();

app.Run();
