using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.SharedKernel.ValueObjects;

/// <summary>
/// A bounded percentage in the range [-100, 100]. Stored as a decimal to preserve precision on
/// margin and commission math. Negative values are allowed because net margins can be negative
/// (selling at a loss); callers that require non-negative percentages (thresholds, commissions,
/// tax rates) MUST validate that at the call site.
/// </summary>
public sealed class Percentage : ValueObject
{
    public const decimal Min = -100m;
    public const decimal Max = 100m;

    public Percentage(decimal value)
    {
        if (value < Min || value > Max)
        {
            throw new ArgumentOutOfRangeException(
                nameof(value), value, $"Percentage must be within [{Min}, {Max}].");
        }

        Value = value;
    }

    public decimal Value { get; }

    public decimal AsFraction => Value / 100m;

    public static Percentage Zero { get; } = new(0m);

    public static Percentage FromFraction(decimal fraction) => new(Math.Round(fraction * 100m, 6));

    public static bool operator <(Percentage left, Percentage right) => left.Value < right.Value;

    public static bool operator >(Percentage left, Percentage right) => left.Value > right.Value;

    public static bool operator <=(Percentage left, Percentage right) => left.Value <= right.Value;

    public static bool operator >=(Percentage left, Percentage right) => left.Value >= right.Value;

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"{Value}%";
}
