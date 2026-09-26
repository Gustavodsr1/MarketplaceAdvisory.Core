using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.SharedKernel.ValueObjects;

/// <summary>
/// Physical weight in grams. Used by the Financial Engine (shipping tier lookup) and by
/// the Bundle Maker (aggregate weight of child SKUs).
/// </summary>
public sealed class Weight : ValueObject
{
    public Weight(int grams)
    {
        if (grams <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(grams), grams, "Weight must be greater than zero grams.");
        }

        Grams = grams;
    }

    public int Grams { get; }

    public static Weight FromGrams(int grams) => new(grams);

    public static Weight FromKilograms(decimal kilograms) => new((int)Math.Round(kilograms * 1000m));

    public static Weight operator +(Weight left, Weight right) => new(left.Grams + right.Grams);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Grams;
    }

    public override string ToString() => $"{Grams} g";
}
