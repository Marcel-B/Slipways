using Microsoft.AspNetCore.DataProtection;
using Prometheus;
using Scalar.AspNetCore;
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
builder.Services.AddHealthChecks().AddDbContextCheck<SlipwaysDbContext>();

// Bearer tokens are protected with data protection keys; persist them so logins survive restarts.
if (builder.Configuration["DataProtection:KeysPath"] is { Length: > 0 } keysPath)
    builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(keysPath));

var app = builder.Build();

// The metrics port answers nothing but /metrics, and the other ports never answer /metrics. It goes by the port the
// connection came in on, not by the Host header, which a client could set. The port is never forwarded to the
// public; Prometheus reaches it through "tailscale serve".
var metricsPort = app.Configuration.GetValue("Monitoring:MetricsPort", new MonitoringOptions().MetricsPort);
app.Use(async (context, next) =>
{
    if ((context.Connection.LocalPort == metricsPort) != (context.Request.Path == "/metrics"))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        return;
    }
    await next(context);
});

// Metrics come first, so they see the status codes the exception handler and status code pages set.
app.UseRouting();
app.UseHttpMetrics();
app.UseExceptionHandler();
app.UseStatusCodePages();
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
// Accounts are off for the read-only public site; Accounts:Enabled=true brings register and login back.
if (app.Configuration.GetValue("Accounts:Enabled", false))
    app.MapAccountEndpoints();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
    await app.Services.InitializeDatabaseAsync();

app.Run();

public partial class Program;
