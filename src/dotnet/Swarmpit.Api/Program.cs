using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Serilog;

using Swarmpit.Api;
using Swarmpit.Api.Auth;
using Swarmpit.Api.Data.CouchDb;
using Swarmpit.Api.Docker;
using DockerOptions = Swarmpit.Api.Docker.DockerOptions;

// Disable legacy claim type mapping so claims like "sub" stay as "sub"
// instead of being mapped to the long-form URI claim types
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
var dbUrl = builder.Configuration["SWARMPIT_DB"] ?? "http://localhost:5984";

// CouchDB
builder.Services.AddHttpClient<CouchDbClient>(client =>
{
    client.BaseAddress = new Uri(dbUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

builder.Services.AddSingleton<SecretRepository>();
builder.Services.AddSingleton<UserRepository>();

// Docker
builder.Services.Configure<DockerOptions>(builder.Configuration.GetSection(DockerOptions.SectionName));
builder.Services.AddSingleton<DockerClientFactory>();
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

        // Resolve signing key dynamically since it's stored in CouchDB
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
