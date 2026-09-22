namespace MarketplaceAdvisory.Integrations.Abstractions.Models;

/// <summary>
/// Canonical, marketplace-agnostic representation of an order. Adapters normalize raw
/// external payloads into this shape so the domain never sees marketplace-specific data.
/// </summary>
public sealed record CanonicalOrder(
    string ExternalOrderId,
    DateTimeOffset PlacedAt,
    decimal Total,
    string Currency);
