using ErrorOr;
using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.Core.Domain.Events;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Catalog;

/// <summary>
/// Catalog product aggregate. Carries every input the Financial Engine (P1), the AI pipeline
/// (P3), Auto-parts (P4) and SAC (P5) need. Sale prices go through <see cref="SetSalePrice"/>,
/// which requires a <c>FloorDecision.Allowed</c> from the guard (Principle VIII).
///
/// v1 note: EF Core still owns a single <c>Price</c> column; the per-marketplace dictionary
/// lands with the Phase 3 schema migration.
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private Product(
        Guid id,
        TenantId tenantId,
        string sku,
        string name,
        Money price,
        Money cmv,
        Weight weight,
        Dimensions dimensions,
        MarketplaceType defaultMarketplace)
        : base(id, tenantId)
    {
        Sku = sku;
        Name = name;
        Price = price;
        Cmv = cmv;
        Weight = weight;
        Dimensions = dimensions;
        DefaultMarketplace = defaultMarketplace;
    }

    private Product() : base(Guid.Empty, default)
    {
        Sku = string.Empty;
        Name = string.Empty;
        Price = Money.Zero("BRL");
        Cmv = Money.Zero("BRL");
        Weight = new Weight(1);
        Dimensions = new Dimensions(1m, 1m, 1m);
        DefaultMarketplace = MarketplaceType.MercadoLivre;
    }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    /// <summary>
    /// Current sale price for <see cref="DefaultMarketplace"/>. Kept as a single column for EF v1;
    /// per-marketplace prices land in Phase 3.
    /// </summary>
    public Money Price { get; private set; }

    /// <summary>Cost of merchandise sold — feeds every profitability calculation.</summary>
    public Money Cmv { get; private set; }

    public Weight Weight { get; private set; }

    public Dimensions Dimensions { get; private set; }

    public MarketplaceType DefaultMarketplace { get; private set; }

    public static ErrorOr<Product> Create(
        TenantId tenantId,
        string sku,
        string name,
        Money cmv,
        Money salePrice,
        Weight weight,
        Dimensions dimensions,
        MarketplaceType defaultMarketplace = MarketplaceType.MercadoLivre)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Error.Validation("Product.Sku", "SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Product.Name", "Name is required.");
        }

        if (cmv.Amount <= 0m)
        {
            return Error.Validation("Product.Cmv", "CMV must be greater than zero.");
        }

        return new Product(
            Guid.NewGuid(), tenantId, sku, name, salePrice, cmv, weight, dimensions, defaultMarketplace);
    }

    /// <summary>
    /// Legacy shortcut used by the existing controller/tests. Creates a product with reasonable
    /// defaults on the Financial Engine inputs. Prefer the full overload in new code so the
    /// Financial Engine has real data.
    /// </summary>
    public static ErrorOr<Product> Create(TenantId tenantId, string sku, string name, Money defaultPrice)
    {
        var cmv = new Money(Math.Max(defaultPrice.Amount / 2m, 0.01m), defaultPrice.Currency);
        return Create(
            tenantId,
            sku,
            name,
            cmv,
            defaultPrice,
            new Weight(100),
            new Dimensions(10m, 10m, 10m));
    }

    public ErrorOr<Success> UpdateCmv(Money newCmv)
    {
        if (newCmv.Amount <= 0m)
        {
            return Error.Validation("Product.Cmv", "CMV must be greater than zero.");
        }

        Cmv = newCmv;
        RaiseDomainEvent(new ProductInputsChanged(Id, nameof(Cmv), DateTimeOffset.UtcNow));
        return Result.Success;
    }

    /// <summary>
    /// Applies a new sale price after the caller has consulted the guard. Any decision other
    /// than <see cref="FloorDecision.Allowed"/> MUST reject the write.
    /// </summary>
    public ErrorOr<Success> SetSalePrice(
        MarketplaceType marketplace,
        Money price,
        FloorDecision floorDecision,
        HumanOverrideToken? overrideToken = null)
    {
        switch (floorDecision)
        {
            case FloorDecision.Allowed allowed:
                if (allowed.Price.Amount != price.Amount)
                {
                    return Error.Validation(
                        "Product.SalePrice.Mismatch",
                        "Approved price does not match the price being applied.");
                }

                break;

            case FloorDecision.RequiresOverride requires when overrideToken is not null:
                // Principle VIII: a human explicitly accepted the loss — audit the override.
                RaiseDomainEvent(new ProfitabilityFloorHit(
                    TenantId, Id, marketplace, overrideToken.ActorId, price, requires.Floor, DateTimeOffset.UtcNow));
                break;

            default:
                return Error.Conflict(
                    "Product.SalePrice.FloorGuard",
                    "Sale price rejected by the Profitability Floor Guard.");
        }

        Price = price;
        DefaultMarketplace = marketplace;
        RaiseDomainEvent(new ProductInputsChanged(Id, $"{nameof(Price)}[{marketplace}]", DateTimeOffset.UtcNow));
        return Result.Success;
    }

    /// <summary>
    /// Legacy shortcut kept for the existing controller/tests. New callers should always go
    /// through <see cref="SetSalePrice"/> so the floor guard is consulted.
    /// </summary>
    public void ChangePrice(Money newPrice) => Price = newPrice;
}

