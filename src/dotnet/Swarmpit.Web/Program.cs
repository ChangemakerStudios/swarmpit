using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

using Swarmpit.Core.Application.Docker;
using Swarmpit.Core.Application.Registries;
using Swarmpit.Core.Application.Stacks;
using Swarmpit.Core.Application.Users;
using Swarmpit.Core.Domain;
using Swarmpit.Core.Domain.Data;
using Swarmpit.Core.Infrastructure.Auth;
using Swarmpit.Core.Infrastructure.CouchDb;
using Swarmpit.Core.Infrastructure.Docker;

// Disable legacy claim type mapping so claims like "sub" stay as "sub"
JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();
JwtSecurityTokenHandler.DefaultOutboundClaimTypeMap.Clear();

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();

builder.Host.UseSerilog();

// Configuration
var couchDbOptions = builder.Configuration.GetSection(CouchDbOptions.SectionName).Get<CouchDbOptions>()
    ?? new CouchDbOptions();

// Allow legacy env var override
var dbUrl = builder.Configuration["SWARMPIT_DB"];
if (!string.IsNullOrEmpty(dbUrl))
{
    couchDbOptions.Url = dbUrl;
}

builder.Services.Configure<CouchDbOptions>(opts =>
{
    opts.Url = couchDbOptions.Url;
    opts.DatabaseName = couchDbOptions.DatabaseName;
    opts.TimeoutSeconds = couchDbOptions.TimeoutSeconds;
});

// CouchDB
builder.Services.AddHttpClient<CouchDbClient>(client =>
{
    client.BaseAddress = new Uri(couchDbOptions.Url);
    client.Timeout = TimeSpan.FromSeconds(couchDbOptions.TimeoutSeconds);
});

builder.Services.AddSingleton<Swarmpit.Core.Application.Users.ISecretRepository, CouchDbSecretRepository>();
builder.Services.AddSingleton<IUserRepository, CouchDbUserRepository>();
builder.Services.AddSingleton<IStackFileRepository, CouchDbStackFileRepository>();
builder.Services.AddSingleton<IRegistryRepository, CouchDbRegistryRepository>();

// Docker
builder.Services.Configure<DockerOptions>(builder.Configuration.GetSection(DockerOptions.SectionName));
builder.Services.AddSingleton<DockerClientFactory>();
builder.Services.AddSingleton<DockerRepository>();
builder.Services.AddSingleton<IServiceRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<INodeRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<INetworkRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<IVolumeRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<Swarmpit.Core.Application.Docker.ISecretRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<IConfigRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<ITaskRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<IContainerRepository>(sp => sp.GetRequiredService<DockerRepository>());
builder.Services.AddSingleton<IStackDeployService, StackDeployService>();
builder.Services.AddSingleton<IComposeGeneratorService, ComposeGeneratorService>();
builder.Services.AddHealthChecks()
    .AddCheck<DockerHealthCheck>("docker", tags: ["ready"]);

// JWT Auth
builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers = [AppConstants.AppIssuer, AppConstants.ApiIssuer],
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "sub",
            RoleClaimType = "role"
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = async context =>
            {
                var jwtService = context.HttpContext.RequestServices.GetRequiredService<JwtService>();
                var key = await jwtService.GetSigningKeyAsync();
                context.Options.TokenValidationParameters.IssuerSigningKey = key;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi();

// CORS for React dev server
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8501")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Serve React static files in production
app.UseDefaultFiles();
app.UseStaticFiles();

app.MapControllers();
app.MapHealthChecks("/healthz");
app.MapHealthChecks("/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

// SPA fallback: any unmatched route serves index.html
app.MapFallbackToFile("index.html");

// Initialize database on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CouchDbClient>();
    try
    {
        await db.EnsureDatabaseAsync();
        Log.Information("CouchDB connection established");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "CouchDB not available at startup — will retry on first request");
    }
}

Log.Information("Swarmpit starting on port {Port}", builder.Configuration["ASPNETCORE_URLS"] ?? "8080");
app.Run();
