using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Events;

/// <summary>Emitted whenever a Product's cost, weight, dimensions or SKU-level pricing input changes.</summary>
public sealed record ProductInputsChanged(Guid ProductId, string ChangedFields, DateTimeOffset OccurredOn) : IDomainEvent;

/// <summary>Emitted after a successful <c>ProfitabilityCalculatorService.Calculate</c> run.</summary>
public sealed record ProfitabilityCalculated(
    Guid ProductId,
    MarketplaceType Marketplace,
    ProfitabilityStatus Status,
    DateTimeOffset OccurredOn) : IDomainEvent;

/// <summary>
/// Emitted every time the Profitability Floor Guard blocks (or would block) a write. Principle VIII
/// requires the seller to be notified and the system to audit the attempt.
/// </summary>
public sealed record ProfitabilityFloorHit(
    TenantId TenantId,
    Guid ProductId,
    MarketplaceType Marketplace,
    string Actor,
    Money ObservedPrice,
    Money Floor,
    DateTimeOffset OccurredOn) : IDomainEvent;

/// <summary>Emitted when a tenant updates its profitability thresholds.</summary>
public sealed record TenantThresholdsChanged(TenantId TenantId, DateTimeOffset OccurredOn) : IDomainEvent;

/// <summary>Emitted when a tenant switches tax regime.</summary>
public sealed record TenantTaxRegimeChanged(TenantId TenantId, TaxRegime NewRegime, DateTimeOffset OccurredOn) : IDomainEvent;
