using FluentAssertions;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Tests;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidData_ReturnsProduct()
    {
        // Arrange
        var tenantId = TenantId.New();
        var price = new Money(199.90m, "BRL");

        // Act
        var cmv = new Money(price.Amount * 0.5m, price.Currency);
        var result = Product.Create(
            tenantId,
            "SKU-001",
            "Brake Pad",
            cmv,
            price,
            new Weight(100),
            new Dimensions(10m, 10m, 10m));

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Sku.Should().Be("SKU-001");
        result.Value.TenantId.Should().Be(tenantId);
    }

    [Fact]
    public void Create_WithEmptySku_ReturnsValidationError()
    {
        // Arrange
        var price = new Money(10m, "BRL");

        // Act
        var cmv = new Money(price.Amount * 0.5m, price.Currency);
        var result = Product.Create(
            TenantId.New(),
            "   ",
            "Brake Pad",
            cmv,
            price,
            new Weight(100),
            new Dimensions(10m, 10m, 10m));

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Product.Sku");
    }
}
