using ErrorOr;
using MarketplaceAdvisory.Contracts.Financial;
using MarketplaceAdvisory.Core.Application.Common.Messaging;

namespace MarketplaceAdvisory.Core.Application.Financial.Queries.RunIdealPriceSimulator;

/// <summary>
/// Ideal-Price Simulator (Rule A3). Given a target net margin, returns the sale price that
/// yields it. Read-only — does not persist anything.
/// </summary>
public sealed record RunIdealPriceSimulatorQuery(
    Guid ProductId,
    string Marketplace,
    decimal TargetNetMarginPercent) : IQuery<ErrorOr<SimulatorResponseDto>>;
