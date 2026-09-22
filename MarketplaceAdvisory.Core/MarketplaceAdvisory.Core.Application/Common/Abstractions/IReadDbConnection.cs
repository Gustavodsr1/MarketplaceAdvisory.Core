using System.Data;

namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Read-side connection factory for high-performance Dapper queries (CQRS read model).
/// The concrete Npgsql connection is created in Infrastructure.
/// </summary>
public interface IReadDbConnection
{
    IDbConnection CreateConnection();
}
