using System.Data;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using Npgsql;

namespace MarketplaceAdvisory.Core.Infrastructure.Persistence;

/// <summary>
/// Creates read-only Npgsql connections for Dapper queries (CQRS read side).
/// </summary>
public sealed class ReadDbConnection(string connectionString) : IReadDbConnection
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
