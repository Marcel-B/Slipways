using Microsoft.AspNetCore.DataProtection;
using Prometheus;
using Scalar.AspNetCore;
using Slipways.Api;
using Slipways.Api.Endpoints;
using Slipways.Infrastructure;
using Slipways.Infrastructure.Monitoring;
using Slipways.Infrastructure.Persistence;

// UseHttpMetrics already measures the requests; the built-in ASP.NET Core meters would only repeat that under other names.
Metrics.SuppressDefaultMetrics(new SuppressDefaultMetricOptions { SuppressMeters = true });
SlipwaysMetrics.Register();

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddAuthorization();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddApiRateLimiting();
builder.Services.AddHealthChecks().AddDbContextCheck<SlipwaysDbContext>();

// Bearer tokens are protected with data protection keys; persist them so logins survive restarts.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));

var app = builder.Build();

// The client address from X-Forwarded-For, for the rate limiter; only trusted from the proxies in the Docker network.
app.UseForwardedHeaders();

// The metrics port answers nothing but /metrics and the moderation port nothing but the moderation; the other ports
// answer neither. It goes by the port the connection came in on, not by the Host header, which a client could set.
// Neither port is ever forwarded to the public; both reach the tailnet through "tailscale serve".
var metricsPort = app.Configuration.GetValue("Monitoring:MetricsPort", new MonitoringOptions().MetricsPort);
var moderationPort = app.Configuration.GetValue("Moderation:Port", 9465);
app.Use(async (context, next) =>
{
    var path = context.Request.Path;
    var moderation = path.StartsWithSegments(ModerationEndpoints.PagePath) || path.StartsWithSegments(ModerationEndpoints.ApiPath);
    if ((context.Connection.LocalPort == metricsPort) != (path == "/metrics")
        || (context.Connection.LocalPort == moderationPort) != moderation)
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    if (moderation)
    {
        context.Response.Headers.ContentSecurityPolicy =
            "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self'; connect-src 'self'; frame-ancestors 'none'; form-action 'none'";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers.CacheControl = "no-store";
    }
    await next(context);
});

// Metrics come first, so they see the status codes the exception handler and status code pages set.
app.UseRouting();
app.UseHttpMetrics();
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapHealthChecks("/health");
app.MapMetrics();
app.MapCatalogEndpoints();
app.MapSuggestionEndpoints();
app.MapModerationEndpoints();
// Accounts are off for the read-only public site; Accounts:Enabled=true brings register and login back.
if (app.Configuration.GetValue("Accounts:Enabled", false))
    app.MapAccountEndpoints();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
    await app.Services.InitializeDatabaseAsync();

app.Run();

public partial class Program;
