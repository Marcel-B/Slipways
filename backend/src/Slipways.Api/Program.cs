using Microsoft.AspNetCore.DataProtection;
using Scalar.AspNetCore;
using Slipways.Api.Endpoints;
using Slipways.Infrastructure;
using Slipways.Infrastructure.Persistence;

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
app.MapCatalogEndpoints();
app.MapAccountEndpoints();

if (app.Configuration.GetValue("Database:InitializeOnStartup", true))
    await app.Services.InitializeDatabaseAsync();

app.Run();

public partial class Program;
