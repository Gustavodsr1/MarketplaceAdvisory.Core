using ErrorOr;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Assembles the full <see cref="ProfitabilityInputs"/> for a product on a marketplace at a
/// given sale price — resolving the active <c>MarketplaceFee</c>, <c>ShippingTier</c>, tenant
/// thresholds and tax regime.
///
/// This is the seam between the (unblocked) Financial Engine and the (Phase-3, DB-backed)
/// fee/shipping/tenant persistence. The concrete implementation lands once the schema and
/// connection string are supplied; command/query handlers and their tests depend only on this
/// abstraction, so the whole vertical is testable today with a fake assembler.
/// </summary>
public interface IProfitabilityInputsAssembler
{
    Task<ErrorOr<ProfitabilityInputs>> AssembleAsync(
        Product product,
        MarketplaceType marketplace,
        Money salePrice,
        CancellationToken cancellationToken);
}
