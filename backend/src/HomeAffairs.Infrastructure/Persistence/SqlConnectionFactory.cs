using System.Data.Common;
using HomeAffairs.Application.Abstractions.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace HomeAffairs.Infrastructure.Persistence;

internal sealed class SqlConnectionFactory
    : ISqlConnectionFactory
{
    private readonly string _connectionString;

    public SqlConnectionFactory(
        IOptions<DatabaseOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var databaseOptions = options.Value;

        if (string.IsNullOrWhiteSpace(
                databaseOptions.ConnectionString))
        {
            throw new InvalidOperationException(
                "The database connection string has not been configured.");
        }

        _connectionString = databaseOptions.ConnectionString;
    }

    public async Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default)
    {
        var connection = new SqlConnection(_connectionString);

        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
