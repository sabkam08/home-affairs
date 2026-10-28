using System.Data.Common;

namespace HomeAffairs.Application.Abstractions.Persistence;

public interface ISqlConnectionFactory
{
    Task<DbConnection> OpenConnectionAsync(
        CancellationToken cancellationToken = default);
}
