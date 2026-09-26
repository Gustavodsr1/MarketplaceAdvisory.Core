using FluentAssertions;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Core.Domain.Tenants;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tests.Tenants;

public sealed class TenantTests
{
    private static ProfitabilityThresholds Defaults => ProfitabilityThresholds.Default;

    private static TaxRegimeSelection InitialRegime => new(
        TaxRegime.SimplesNacional,
        new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));

    [Fact]
    public void Register_WithValidData_Succeeds()
    {
        var result = Tenant.Register(TenantId.New(), "Acme Autopeças", Defaults, InitialRegime);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Acme Autopeças");
        result.Value.Thresholds.Should().Be(Defaults);
    }

    [Fact]
    public void Register_WithEmptyName_ReturnsValidationError()
    {
        var result = Tenant.Register(TenantId.New(), "  ", Defaults, InitialRegime);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Tenant.Name");
    }

    [Fact]
    public void UpdateThresholds_RaisesEvent()
    {
        var tenant = Tenant.Register(TenantId.New(), "Acme", Defaults, InitialRegime).Value;

        var newThresholds = new ProfitabilityThresholds(
            new Percentage(20m), new Percentage(10m), new Percentage(5m));

        tenant.UpdateThresholds(newThresholds).IsError.Should().BeFalse();

        tenant.Thresholds.Green.Value.Should().Be(20m);
        tenant.DomainEvents.Should().ContainSingle(e => e.GetType().Name == "TenantThresholdsChanged");
    }

    [Fact]
    public void ChangeTaxRegime_OverlappingWindow_Fails()
    {
        var tenant = Tenant.Register(TenantId.New(), "Acme", Defaults, InitialRegime).Value;

        var overlapping = new TaxRegimeSelection(
            TaxRegime.LucroPresumido,
            new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero));

        var result = tenant.ChangeTaxRegime(overlapping);

        result.IsError.Should().BeTrue();
        result.FirstError.Description.Should().Contain("overlap");
    }

    [Fact]
    public void ChangeTaxRegime_NonOverlappingWindow_Succeeds()
    {
        var tenant = Tenant.Register(
            TenantId.New(),
            "Acme",
            Defaults,
            new TaxRegimeSelection(
                TaxRegime.SimplesNacional,
                new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero))).Value;

        var next = new TaxRegimeSelection(
            TaxRegime.LucroPresumido,
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        var result = tenant.ChangeTaxRegime(next);

        result.IsError.Should().BeFalse();
        tenant.TaxRegimeHistory.Should().HaveCount(2);
    }
}
