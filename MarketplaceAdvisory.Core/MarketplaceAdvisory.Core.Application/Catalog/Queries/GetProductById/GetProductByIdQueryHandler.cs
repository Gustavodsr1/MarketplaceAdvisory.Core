using Dapper;
using ErrorOr;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Application.Common.Messaging;
using MarketplaceAdvisory.Contracts.Catalog;

namespace MarketplaceAdvisory.Core.Application.Catalog.Queries.GetProductById;

/// <summary>
/// Read-side handler that queries the read model directly with Dapper (bypassing EF Core)
/// for maximum read performance.
/// </summary>
public sealed class GetProductByIdQueryHandler(IReadDbConnection readDbConnection)
    : IQueryHandler<GetProductByIdQuery, ErrorOr<ProductDto>>
{
    public async Task<ErrorOr<ProductDto>> Handle(
        GetProductByIdQuery request,
        CancellationToken cancellationToken)
    {
        using var connection = readDbConnection.CreateConnection();

        const string sql =
            """
            SELECT id           AS Id,
                   sku          AS Sku,
                   name         AS Name,
                   price_amount AS Price,
                   price_currency AS Currency
            FROM catalog.products
            WHERE id = @Id
            """;

        var product = await connection.QueryFirstOrDefaultAsync<ProductDto>(
            new CommandDefinition(sql, new { request.Id }, cancellationToken: cancellationToken));

        return product is null
            ? Error.NotFound("Product.NotFound", $"Product '{request.Id}' was not found.")
            : product;
    }
}
