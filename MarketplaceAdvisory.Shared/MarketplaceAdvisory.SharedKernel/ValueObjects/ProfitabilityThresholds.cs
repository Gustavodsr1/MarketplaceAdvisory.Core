using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.SharedKernel.ValueObjects;

/// <summary>
/// Tenant-configurable profitability thresholds enforcing Principle VIII
/// ("Profitability Guardian"). The invariant is <c>Green ≥ Yellow ≥ Floor ≥ 0</c>.
/// </summary>
public sealed class ProfitabilityThresholds : ValueObject
{
    public ProfitabilityThresholds(Percentage green, Percentage yellow, Percentage minimumFloor)
    {
        if (green is null) throw new ArgumentNullException(nameof(green));
        if (yellow is null) throw new ArgumentNullException(nameof(yellow));
        if (minimumFloor is null) throw new ArgumentNullException(nameof(minimumFloor));

        if (green < yellow || yellow < minimumFloor || minimumFloor.Value < 0m)
        {
            throw new ArgumentException(
                $"Thresholds must satisfy Green ({green}) >= Yellow ({yellow}) >= Floor ({minimumFloor}) >= 0.");
        }

        Green = green;
        Yellow = yellow;
        MinimumFloor = minimumFloor;
    }

    public Percentage Green { get; }

    public Percentage Yellow { get; }

    public Percentage MinimumFloor { get; }

    /// <summary>
    /// Reasonable platform defaults used when a tenant has not customized them.
    /// </summary>
    public static ProfitabilityThresholds Default { get; } =
        new(new Percentage(12m), new Percentage(6m), new Percentage(0m));

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Green.Value;
        yield return Yellow.Value;
        yield return MinimumFloor.Value;
    }

    public override string ToString() =>
        $"green={Green}, yellow={Yellow}, floor={MinimumFloor}";
}
