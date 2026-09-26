using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Behavioral contract for a Brazilian tax regime. The <see cref="ProfitabilityCalculatorService"/>
/// only knows this abstraction so we can add regimes (MEI, Lucro Real) later without touching
/// the calculator.
/// </summary>
public interface ITaxRegime
{
    TaxRegime Kind { get; }

    /// <summary>
    /// Effective tax percentage applied to the sale price for the given inputs. Returned as
    /// a fraction of the sale price (never negative, never above 100%).
    /// </summary>
    Percentage EffectiveRate(TaxRegimeContext context);
}

/// <summary>
/// Inputs a tax regime needs to resolve its effective rate.
/// </summary>
public sealed record TaxRegimeContext(Money SalePrice, string? ActivityCode = null);

/// <summary>
/// Simples Nacional applies a flat percentage over the sale price. The rate is configured per
/// tenant via <c>TaxProfile</c>.
/// </summary>
public sealed class SimplesNacionalRegime : ITaxRegime
{
    public SimplesNacionalRegime(Percentage flatRate)
    {
        FlatRate = flatRate ?? throw new ArgumentNullException(nameof(flatRate));
    }

    public TaxRegime Kind => TaxRegime.SimplesNacional;

    public Percentage FlatRate { get; }

    public Percentage EffectiveRate(TaxRegimeContext context) => FlatRate;
}

/// <summary>
/// Lucro Presumido carries different rates per activity code. The tenant configures the
/// bracket table via <c>TaxProfile</c>.
/// </summary>
public sealed class LucroPresumidoRegime : ITaxRegime
{
    private readonly IReadOnlyDictionary<string, Percentage> _brackets;
    private readonly Percentage _defaultRate;

    public LucroPresumidoRegime(IReadOnlyDictionary<string, Percentage> brackets, Percentage defaultRate)
    {
        _brackets = brackets ?? throw new ArgumentNullException(nameof(brackets));
        _defaultRate = defaultRate ?? throw new ArgumentNullException(nameof(defaultRate));
    }

    public TaxRegime Kind => TaxRegime.LucroPresumido;

    public Percentage EffectiveRate(TaxRegimeContext context)
    {
        if (!string.IsNullOrWhiteSpace(context.ActivityCode)
            && _brackets.TryGetValue(context.ActivityCode!, out var bracket))
        {
            return bracket;
        }

        return _defaultRate;
    }
}
