using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.SharedKernel.ValueObjects;

/// <summary>
/// Box dimensions in centimeters. Used by shipping calculators to derive volumetric weight
/// and by the Bundle Maker to compute aggregate dimensions per FR-B3 (Max/Max/Sum rule).
/// </summary>
public sealed class Dimensions : ValueObject
{
    public Dimensions(decimal widthCm, decimal heightCm, decimal lengthCm)
    {
        if (widthCm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(widthCm), widthCm, "Width must be greater than zero centimeters.");
        }

        if (heightCm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(heightCm), heightCm, "Height must be greater than zero centimeters.");
        }

        if (lengthCm <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(lengthCm), lengthCm, "Length must be greater than zero centimeters.");
        }

        WidthCm = widthCm;
        HeightCm = heightCm;
        LengthCm = lengthCm;
    }

    public decimal WidthCm { get; }

    public decimal HeightCm { get; }

    public decimal LengthCm { get; }

    /// <summary>
    /// Marketplace-standard volumetric weight in grams. Result is
    /// <c>(width * height * length) / divisor</c> in centimeters divided by a marketplace-specific
    /// divisor (Mercado Livre uses 6000, Correios uses 6000 as well). Callers pass the divisor
    /// so the domain stays marketplace-agnostic.
    /// </summary>
    public int VolumetricWeightGrams(int divisor)
    {
        if (divisor <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(divisor), divisor, "Divisor must be greater than zero.");
        }

        var volume = WidthCm * HeightCm * LengthCm;
        var kilograms = volume / divisor;
        return (int)Math.Round(kilograms * 1000m);
    }

    /// <summary>
    /// Bundle rule (FR-B3): the two largest sides are the max of the children's sides and the
    /// remaining side is the sum. Callers pass the ordered list of sides — the domain assumes
    /// callers already know which axis to sum.
    /// </summary>
    public static Dimensions CombineForBundle(IEnumerable<Dimensions> parts)
    {
        var enumerated = parts?.ToArray() ?? throw new ArgumentNullException(nameof(parts));
        if (enumerated.Length < 2)
        {
            throw new ArgumentException("At least two dimensions are required to combine.", nameof(parts));
        }

        // Sort each dimension's three sides descending so the top two are "max" candidates and
        // the third is the "sum" axis. This mirrors the acceptance test example
        // (20+15+(10+5) = 50).
        var maxSideA = enumerated.Max(d => Math.Max(Math.Max(d.WidthCm, d.HeightCm), d.LengthCm));
        var maxSideB = enumerated.Max(d => Median(d.WidthCm, d.HeightCm, d.LengthCm));
        var sumSideC = enumerated.Sum(d => Math.Min(Math.Min(d.WidthCm, d.HeightCm), d.LengthCm));

        return new Dimensions(maxSideA, maxSideB, sumSideC);
    }

    private static decimal Median(decimal a, decimal b, decimal c)
    {
        var max = Math.Max(a, Math.Max(b, c));
        var min = Math.Min(a, Math.Min(b, c));
        return a + b + c - max - min;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return WidthCm;
        yield return HeightCm;
        yield return LengthCm;
    }

    public override string ToString() => $"{WidthCm}x{HeightCm}x{LengthCm} cm";
}
