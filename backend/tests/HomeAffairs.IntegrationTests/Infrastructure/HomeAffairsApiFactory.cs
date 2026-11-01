using HomeAffairs.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MsSql;

namespace HomeAffairs.IntegrationTests.Infrastructure;

public sealed class HomeAffairsApiFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder()
            .WithPassword(
                "Integration-Test-Sql-2026!")
            .Build();

    public HttpClient Client { get; private set; } =
        null!;

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        await InitializeDatabaseAsync();

        var databaseName =
            await QueryScalarAsync<string>(
                "SELECT DB_NAME();");

                if (!string.Equals(
                        databaseName,
                        "HomeAffairsDb",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Integration database is '{databaseName}', " +
                        "but HomeAffairsDb was expected.");
                }

        Client = CreateClient();
    }

    public new async Task DisposeAsync()
    {
        Client?.Dispose();

        await _sqlContainer.DisposeAsync();

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(
    IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "Database:ConnectionString",
            BuildDatabaseConnectionString());

        builder.UseSetting(
            "Database:CommandTimeoutSeconds",
            "30");

        builder.UseSetting(
            "Jwt:Issuer",
            "HomeAffairs.IntegrationTests");

        builder.UseSetting(
            "Jwt:Audience",
            "HomeAffairs.IntegrationTests");

        builder.UseSetting(
            "Jwt:SigningKey",
            "HomeAffairs-Integration-Tests-2026-Signing-Key-Do-Not-Use-In-Production-123456");

        builder.UseSetting(
            "Jwt:AccessTokenLifetimeMinutes",
            "15");

        builder.UseSetting(
            "Jwt:RefreshTokenLifetimeDays",
            "7");

        builder.UseSetting(
            "IdentitySecurity:MaximumFailedLoginAttempts",
            "5");

        builder.UseSetting(
            "IdentitySecurity:LockoutDurationMinutes",
            "15");
    }

    private string BuildDatabaseConnectionString()
    {
        var builder =
            new Microsoft.Data.SqlClient
                .SqlConnectionStringBuilder(
                    _sqlContainer.GetConnectionString())
            {
                InitialCatalog = "HomeAffairsDb",
                TrustServerCertificate = true
            };

        return builder.ConnectionString;
    }

    private async Task InitializeDatabaseAsync()
    {
        var root =
            FindRepositoryRoot();

        await ExecuteScriptAsync(
            Path.Combine(
                root,
                "database",
                "001_database.sql"),
            databaseName: "master");

        for (var script = 2; script <= 10; script++)
        {
            var prefix =
                script.ToString("000");

            var file =
                Directory
                    .EnumerateFiles(
                        Path.Combine(root, "database"),
                        $"{prefix}_*.sql")
                    .Single();

            await ExecuteScriptAsync(
                file,
                databaseName: "HomeAffairsDb");
        }
    }

    private async Task ExecuteScriptAsync(
        string path,
        string databaseName)
    {
        var connectionStringBuilder =
            new Microsoft.Data.SqlClient
                .SqlConnectionStringBuilder(
                    _sqlContainer.GetConnectionString())
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true
            };

        await using var connection =
            new Microsoft.Data.SqlClient.SqlConnection(
                connectionStringBuilder.ConnectionString);

        await connection.OpenAsync();

        var script =
            await File.ReadAllTextAsync(path);

        /*
            Our setup scripts use GO batch separators.
            SqlCommand itself does not understand GO, so
            execute each batch separately.
        */
        var batches =
            System.Text.RegularExpressions.Regex.Split(
                script,
                @"^\s*GO\s*;?\s*$",
                System.Text.RegularExpressions
                    .RegexOptions.Multiline
                | System.Text.RegularExpressions
                    .RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText = batch;
            command.CommandTimeout = 60;

            await command.ExecuteNonQueryAsync();
        }
    }

    private static string FindRepositoryRoot()
    {
        var directory =
            new DirectoryInfo(
                AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (Directory.Exists(
                    Path.Combine(
                        directory.FullName,
                        "database")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the backend repository root.");
    }
}
