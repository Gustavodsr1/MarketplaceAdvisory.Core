using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Messaging;

namespace MarketplaceAdvisory.Core.Application.Financial.Queries.GetProductProfitability;

/// <summary>
/// Returns the current profitability projection for a SKU. v1 returns a single entry for the
/// product's default marketplace; the per-marketplace array lands with the Phase 3 read model.
/// </summary>
public sealed record GetProductProfitabilityQuery(Guid ProductId)
    : IQuery<ErrorOr<IReadOnlyList<ProductProfitabilityDto>>>;
