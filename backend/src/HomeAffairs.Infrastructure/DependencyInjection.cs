using HomeAffairs.Application.Abstractions.Identity;
using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Application.Abstractions.Time;
using HomeAffairs.Infrastructure.Identity;
using HomeAffairs.Infrastructure.Persistence;
using HomeAffairs.Infrastructure.Time;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HomeAffairs.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Bind(configuration.GetSection(
                DatabaseOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(
                        options.ConnectionString),
                "Database connection string is required.")
            .Validate(
                options =>
                    options.CommandTimeoutSeconds > 0,
                "Database command timeout must be greater than zero.")
            .ValidateOnStart();

        services
            .AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(
                JwtOptions.SectionName))
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Issuer),
                "JWT issuer is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.Audience),
                "JWT audience is required.")
            .Validate(
                options =>
                    !string.IsNullOrWhiteSpace(options.SigningKey)
                    && options.SigningKey.Length >= 32,
                "JWT signing key must contain at least 32 characters.")
            .Validate(
                options =>
                    options.AccessTokenLifetimeMinutes > 0,
                "JWT access-token lifetime must be greater than zero.")
            .Validate(
                options =>
                    options.RefreshTokenLifetimeDays > 0,
                    "JWT refresh-token lifetime must be greater than zero.")
            .ValidateOnStart();

        services
            .AddOptions<IdentitySecuritySettings>()
            .Bind(configuration.GetSection(
                IdentitySecuritySettings.SectionName))
            .Validate(
                options =>
                    options.MaximumFailedLoginAttempts > 0,
                "Maximum failed login attempts must be greater than zero.")
            .Validate(
                options =>
                    options.LockoutDurationMinutes > 0,
                "Lockout duration must be greater than zero.")
            .ValidateOnStart();
               
        services.AddSingleton<
            IRefreshTokenGenerator,
            RefreshTokenGenerator>();

        services.AddScoped<
            IRefreshTokenRepository,
            RefreshTokenRepository>();

        services.AddScoped<
            IRefreshTokenIssuer,
            RefreshTokenIssuer>();

        services.AddSingleton<
            ISqlConnectionFactory,
            SqlConnectionFactory>();

        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<
            ISecurityEventRepository,
            SecurityEventRepository>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<
            IAccessTokenProvider,
            JwtAccessTokenProvider>();

        services.AddScoped<
            IUserPermissionRepository,
            UserPermissionRepository>();

        services
            .AddHealthChecks()
            .AddCheck<SqlServerHealthCheck>(
                name: "sql-server",
                failureStatus:
                    Microsoft.Extensions.Diagnostics.HealthChecks
                        .HealthStatus.Unhealthy,
                tags: ["ready", "database"]);

        return services;
    }
}
