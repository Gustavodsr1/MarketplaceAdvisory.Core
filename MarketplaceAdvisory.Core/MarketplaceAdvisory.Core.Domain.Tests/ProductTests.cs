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
        var result = Product.Create(tenantId, "SKU-001", "Brake Pad", price);

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
        var result = Product.Create(TenantId.New(), "   ", "Brake Pad", price);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Product.Sku");
    }
}
