namespace MarketplaceAdvisory.Integrations.Abstractions.Models;

/// <summary>
/// Canonical, marketplace-agnostic representation of a product listing/announcement.
/// </summary>
public sealed record CanonicalListing(
    string ExternalListingId,
    string Title,
    decimal Price,
    string Currency);
