using HomeAffairs.Application.Abstractions.Persistence;
using HomeAffairs.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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

        services.AddSingleton<ISqlConnectionFactory,
            SqlConnectionFactory>();

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
