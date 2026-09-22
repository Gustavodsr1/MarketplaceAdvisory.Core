using FluentAssertions;
using MarketplaceAdvisory.Core.Application.Catalog.Commands.CreateProduct;

namespace MarketplaceAdvisory.Core.Application.Tests;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        // Arrange
        var command = new CreateProductCommand("SKU-1", "Brake Pad", 10m, "BRL");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptySku_IsInvalid()
    {
        // Arrange
        var command = new CreateProductCommand(string.Empty, "Brake Pad", 10m, "BRL");

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == nameof(CreateProductCommand.Sku));
    }
}
