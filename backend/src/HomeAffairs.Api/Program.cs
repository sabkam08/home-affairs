using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using HomeAffairs.Api.ExceptionHandling;
using HomeAffairs.Api.Middleware;
using HomeAffairs.Application;
using HomeAffairs.Infrastructure;
using HomeAffairs.Api.Identity;
using HomeAffairs.Api.Authorization;
using HomeAffairs.Application.Authorization;
using HomeAffairs.Application.Abstractions.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithProperty("Application", "HomeAffairs.Api")
        .WriteTo.Console();
});

builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<
    IRequestContext,
    HttpRequestContext>();

builder.Services.AddScoped<
    ICurrentUser,
    CurrentUser>();

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer has not been configured.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience has not been configured.");

var jwtSigningKey =
    builder.Configuration["Jwt:SigningKey"]
    ?? throw new InvalidOperationException(
        "JWT signing key has not been configured.");

builder.Services
    .AddAuthentication(
        JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtIssuer,

                ValidateAudience = true,
                ValidAudience = jwtAudience,

                ValidateIssuerSigningKey = true,
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            jwtSigningKey)),

                ValidateLifetime = true,

                ClockSkew = TimeSpan.FromSeconds(30)
            };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(
        AuthorizationPolicies.ProfileReadSelf,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Profile.ReadSelf));
        });

    options.AddPolicy(
        AuthorizationPolicies.ApplicationCreate,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Applications.Create));
        });

    options.AddPolicy(
        AuthorizationPolicies.ApplicationReadAll,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Applications.ReadAll));
        });

    options.AddPolicy(
        AuthorizationPolicies.ApplicationProcess,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Applications.Process));
        });

    options.AddPolicy(
        AuthorizationPolicies.ApplicationApprove,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Applications.Approve));
        });

    options.AddPolicy(
        AuthorizationPolicies.ManageUsers,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Identity.ManageUsers));
        });

    options.AddPolicy(
        AuthorizationPolicies.ManageRoles,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Identity.ManageRoles));
        });

    options.AddPolicy(
        AuthorizationPolicies.AuditRead,
        policy =>
        {
            policy.RequireAuthenticatedUser();

            policy.AddRequirements(
                new PermissionRequirement(
                    Permissions.Audit.Read));
        });
});

builder.Services.AddScoped<
    IAuthorizationHandler,
    PermissionAuthorizationHandler>();

builder.Services.AddProblemDetails();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "HomeAffairs API",
        Version = "v1",
        Description =
            "Home Affairs citizen services, workflow and document management API."
    });
});

builder.Services.AddApplication();

builder.Services.AddInfrastructure(
    builder.Configuration);

builder.Services.AddHealthChecks();

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(resource =>
    {
        resource.AddService("HomeAffairs.Api");
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();
    });

var app = builder.Build();

app.UseExceptionHandler();

app.UseMiddleware<CorrelationIdMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "HomeAffairs API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.MapHealthChecks("/health");

app.MapHealthChecks(
    "/health/live",
    new HealthCheckOptions
    {
        Predicate = _ => false
    });

app.MapHealthChecks(
    "/health/ready",
    new HealthCheckOptions
    {
        Predicate = registration =>
            registration.Tags.Contains("ready")
    });

app.Run();

public partial class Program;
