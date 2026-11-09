using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace HomeAffairs.IntegrationTests.Infrastructure;

public sealed class HomeAffairsApiFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer =
        new MsSqlBuilder(
                "mcr.microsoft.com/mssql/server:2025-latest")
            .WithPassword(
                "Integration-Test-Sql-2026!")
            .Build();

    public HttpClient Client { get; private set; } =
        null!;

    public string DatabaseConnectionString =>
        BuildDatabaseConnectionString();

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        await InitializeDatabaseAsync();

        var actualDatabaseName =
            await QueryScalarAsync<string>(
                "SELECT DB_NAME();");

        if (!string.Equals(
                actualDatabaseName,
                "HomeAffairsDb",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Integration database is " +
                $"'{actualDatabaseName}', but " +
                "'HomeAffairsDb' was expected.");
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
            "HomeAffairs-Integration-Tests-2026-" +
            "Signing-Key-Do-Not-Use-In-Production-123456");

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

    public async Task ExecuteSqlAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(
                BuildDatabaseConnectionString());

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;
        command.CommandTimeout = 60;

        await command.ExecuteNonQueryAsync(
            cancellationToken);
    }

    public async Task<T?> QueryScalarAsync<T>(
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(
                BuildDatabaseConnectionString());

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;
        command.CommandTimeout = 60;

        var result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null ||
            result is DBNull)
        {
            return default;
        }

        if (result is T typedResult)
        {
            return typedResult;
        }

        return (T)Convert.ChangeType(
            result,
            typeof(T));
    }

    public async Task<string?> QueryStringAsync(
        string sql,
        CancellationToken cancellationToken = default)
    {
        await using var connection =
            new SqlConnection(
                BuildDatabaseConnectionString());

        await connection.OpenAsync(
            cancellationToken);

        await using var command =
            connection.CreateCommand();

        command.CommandText = sql;
        command.CommandTimeout = 60;

        var result =
            await command.ExecuteScalarAsync(
                cancellationToken);

        if (result is null ||
            result is DBNull)
        {
            return null;
        }

        return Convert.ToString(result);
    }

    private string BuildDatabaseConnectionString()
    {
        var builder =
            new SqlConnectionStringBuilder(
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

        /*
            Script 001 creates HomeAffairsDb,
            therefore it executes against master.
        */
        await ExecuteScriptAsync(
            Path.Combine(
                root,
                "database",
                "001_database.sql"),
            databaseName: "master");

        /*
            Scripts 002 through 010 execute against
            HomeAffairsDb.

            Files beneath database/dev are intentionally
            excluded from integration-test provisioning.
        */
        for (var scriptNumber = 2;
             scriptNumber <= 24;
             scriptNumber++)
        {
            var prefix =
                scriptNumber.ToString("000");

            var databaseDirectory =
                Path.Combine(
                    root,
                    "database");

            var matches =
                Directory
                    .EnumerateFiles(
                        databaseDirectory,
                        $"{prefix}_*.sql",
                        SearchOption.TopDirectoryOnly)
                    .ToArray();

            if (matches.Length != 1)
            {
                throw new InvalidOperationException(
                    $"Expected exactly one database script " +
                    $"matching {prefix}_*.sql, but found " +
                    $"{matches.Length}.");
            }

            await ExecuteScriptAsync(
                matches[0],
                databaseName: "HomeAffairsDb");
        }
    }

    private async Task ExecuteScriptAsync(
        string path,
        string databaseName)
    {
        var connectionStringBuilder =
            new SqlConnectionStringBuilder(
                _sqlContainer.GetConnectionString())
            {
                InitialCatalog = databaseName,
                TrustServerCertificate = true
            };

        await using var connection =
            new SqlConnection(
                connectionStringBuilder.ConnectionString);

        await connection.OpenAsync();

        var script =
            await File.ReadAllTextAsync(path);

        /*
            GO is a SQL Server tooling batch separator.
            SqlCommand does not interpret GO itself,
            therefore each batch is executed separately.
        */
        var batches =
            System.Text.RegularExpressions.Regex.Split(
                script,
                @"^\s*GO\s*;?\s*$",
                System.Text.RegularExpressions.RegexOptions.Multiline
                |
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        foreach (var batch in batches)
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command =
                connection.CreateCommand();

            command.CommandText = batch;

            /*
                Schema provisioning can take longer under
                Docker Desktop than normal application
                database operations.

                This timeout is restricted to integration
                database setup.
            */
            command.CommandTimeout = 120;

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
            var databaseDirectory =
                Path.Combine(
                    directory.FullName,
                    "database");

            if (Directory.Exists(
                    databaseDirectory))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException(
            "Could not locate the backend " +
            "repository root.");
    }
}
