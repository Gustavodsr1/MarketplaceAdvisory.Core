namespace MarketplaceAdvisory.Contracts.Financial;

/// <summary>
/// Monetary amount transported across the API boundary. Mirrors the domain <c>Money</c> value
/// object without leaking the domain type.
/// </summary>
public sealed record MoneyDto(decimal Amount, string Currency);
