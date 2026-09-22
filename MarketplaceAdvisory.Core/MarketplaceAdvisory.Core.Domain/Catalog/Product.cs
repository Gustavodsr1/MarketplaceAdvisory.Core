using ErrorOr;
using MarketplaceAdvisory.Core.Domain.Common;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Catalog;

/// <summary>
/// Catalog product aggregate. Sample aggregate that demonstrates the DDD pattern
/// (private setters, factory method returning <see cref="ErrorOr{T}"/>, tenant ownership).
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    private Product(Guid id, TenantId tenantId, string sku, string name, Money price)
        : base(id, tenantId)
    {
        Sku = sku;
        Name = name;
        Price = price;
    }

    // Materialization constructor used by EF Core.
    private Product() : base(Guid.Empty, default)
    {
        Sku = string.Empty;
        Name = string.Empty;
        Price = Money.Zero("BRL");
    }

    public string Sku { get; private set; }

    public string Name { get; private set; }

    public Money Price { get; private set; }

    public static ErrorOr<Product> Create(TenantId tenantId, string sku, string name, Money price)
    {
        if (string.IsNullOrWhiteSpace(sku))
        {
            return Error.Validation("Product.Sku", "SKU is required.");
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Error.Validation("Product.Name", "Name is required.");
        }

        return new Product(Guid.NewGuid(), tenantId, sku, name, price);
    }

    public void ChangePrice(Money newPrice) => Price = newPrice;
}
