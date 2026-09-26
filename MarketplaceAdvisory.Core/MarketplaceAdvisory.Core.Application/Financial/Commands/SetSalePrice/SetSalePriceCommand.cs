using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Messaging;

namespace MarketplaceAdvisory.Core.Application.Financial.Commands.SetSalePrice;

/// <summary>
/// Sets the sale price of a SKU on a marketplace. Consults <c>IProfitabilityFloorGuard</c>
/// (Principle VIII) before persisting. A below-floor write is rejected with
/// <c>floor.override_required</c> unless a sufficient <see cref="SalePriceOverride"/> is supplied.
/// </summary>
public sealed record SetSalePriceCommand(
    Guid ProductId,
    string Marketplace,
    decimal Amount,
    string Currency,
    SalePriceOverride? Override) : ICommand<ErrorOr<ProductProfitabilityDto>>;

/// <summary>
/// Human override accepting a below-floor sale (Principle VIII escape hatch). The accepted loss
/// MUST be at least the gap to the floor, otherwise the write is held.
/// </summary>
public sealed record SalePriceOverride(string ReasonCode, string? ReasonNote, decimal AcceptedLossAmount);
