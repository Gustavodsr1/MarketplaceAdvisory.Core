using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MarketplaceAdvisory.Core.Infrastructure.Persistence.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(product => product.TenantId)
            .HasColumnName("tenant_id")
            .HasConversion(tenantId => tenantId.Value, value => TenantId.From(value))
            .IsRequired();

        builder.HasIndex(product => product.TenantId);

        builder.Property(product => product.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(product => product.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.OwnsOne(product => product.Price, price =>
        {
            price.Property(money => money.Amount)
                .HasColumnName("price_amount")
                .HasColumnType("numeric(18,2)")
                .IsRequired();

            price.Property(money => money.Currency)
                .HasColumnName("price_currency")
                .HasMaxLength(3)
                .IsRequired();
        });

        // Ignored in v1 schema. Phase 3 (T031) will introduce dedicated columns/tables:
        //   Cmv → owned Money (numeric + currency)
        //   Weight → int grams
        //   Dimensions → owned VO (width/height/length)
        //   DefaultMarketplace → int enum
        builder.Ignore(product => product.Cmv);
        builder.Ignore(product => product.Weight);
        builder.Ignore(product => product.Dimensions);
        builder.Ignore(product => product.DefaultMarketplace);

        builder.Ignore(product => product.DomainEvents);
    }
}
