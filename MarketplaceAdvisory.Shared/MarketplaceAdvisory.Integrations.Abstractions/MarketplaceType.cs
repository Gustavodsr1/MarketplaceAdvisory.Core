namespace MarketplaceAdvisory.Integrations.Abstractions;

/// <summary>
/// Supported external marketplaces. New marketplaces are added here and get a dedicated
/// adapter in the Infrastructure layer.
/// </summary>
public enum MarketplaceType
{
    MercadoLivre = 1,
    Shopee = 2,
    Amazon = 3
}
