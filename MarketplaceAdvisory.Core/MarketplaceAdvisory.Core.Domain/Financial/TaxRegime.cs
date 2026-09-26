namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Brazilian tax regimes supported in v1. See <see cref="ITaxRegime"/> for behavioral
/// contract (rate resolution given a set of inputs).
/// </summary>
public enum TaxRegime
{
    SimplesNacional = 1,
    LucroPresumido = 2
}
