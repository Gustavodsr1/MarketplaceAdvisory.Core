using ErrorOr;
using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Core.Domain.Events;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tenants;

/// <summary>
/// Tenant aggregate — carries the tenant-configurable knobs the Financial Engine consults on
/// every calculation: profitability thresholds, tax regime selection and refresh SLAs.
/// </summary>
public sealed class Tenant : AggregateRoot<Guid>
{
    private readonly List<TaxRegimeSelection> _taxRegimeHistory = new();

    private Tenant(
        Guid id,
        TenantId tenantId,
        string name,
        ProfitabilityThresholds thresholds,
        TaxRegimeSelection initialRegime)
        : base(id, tenantId)
    {
        Name = name;
        Thresholds = thresholds;
        _taxRegimeHistory.Add(initialRegime);
    }

    private Tenant() : base(Guid.Empty, default)
    {
        Name = string.Empty;
        Thresholds = ProfitabilityThresholds.Default;
    }

    public string Name { get; private set; }

    public ProfitabilityThresholds Thresholds { get; private set; }

    public IReadOnlyCollection<TaxRegimeSelection> TaxRegimeHistory => _taxRegimeHistory.AsReadOnly();

    public TaxRegimeSelection? CurrentRegimeAt(DateTimeOffset at) =>
        _taxRegimeHistory.FirstOrDefault(r => r.IsActiveOn(at));

    public static ErrorOr<Tenant> Register(
        TenantId tenantId,
        string name,
        ProfitabilityThresholds thresholds,
        TaxRegimeSelection initialRegime)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Tenant.Name", "Name is required.");
        }

        return new Tenant(Guid.NewGuid(), tenantId, name, thresholds, initialRegime);
    }

    public ErrorOr<Success> UpdateThresholds(ProfitabilityThresholds newThresholds)
    {
        if (newThresholds is null)
        {
            return Error.Validation("Tenant.Thresholds", "Thresholds are required.");
        }

        Thresholds = newThresholds;
        RaiseDomainEvent(new TenantThresholdsChanged(TenantId, DateTimeOffset.UtcNow));
        return Result.Success;
    }

    public ErrorOr<Success> ChangeTaxRegime(TaxRegimeSelection newRegime)
    {
        if (newRegime is null)
        {
            return Error.Validation("Tenant.TaxRegime", "Regime selection is required.");
        }

        var overlap = _taxRegimeHistory.FirstOrDefault(existing =>
            newRegime.EffectiveFrom < (existing.EffectiveUntil ?? DateTimeOffset.MaxValue)
            && (newRegime.EffectiveUntil ?? DateTimeOffset.MaxValue) > existing.EffectiveFrom);

        if (overlap is not null)
        {
            return Error.Validation("Tenant.TaxRegime", "New regime window overlaps with an existing selection.");
        }

        _taxRegimeHistory.Add(newRegime);
        RaiseDomainEvent(new TenantTaxRegimeChanged(TenantId, newRegime.Regime, newRegime.EffectiveFrom));
        return Result.Success;
    }
}
