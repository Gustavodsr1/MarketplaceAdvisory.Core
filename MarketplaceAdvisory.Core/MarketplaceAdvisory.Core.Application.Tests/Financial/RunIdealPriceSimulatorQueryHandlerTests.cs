using ErrorOr;
using FluentAssertions;
using MarketplaceAdvisory.Core.Application.Financial.Queries.RunIdealPriceSimulator;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Tests.Financial;

public sealed class RunIdealPriceSimulatorQueryHandlerTests
{
    private static Product NewProduct(TenantId tenant, decimal price = 100m) =>
        Product.Create(tenant, "SKU-1", "Filtro de óleo", new Money(price, "BRL")).Value;

    private static RunIdealPriceSimulatorQueryHandler NewHandler(TenantId tenant, Product product) =>
        new(
            new FakeProductRepository(product),
            new FakeProfitabilityInputsAssembler(),
            new StubTenantContext(tenant),
            new IdealPriceSimulator(),
            new ProfitabilityCalculatorService());

    [Fact]
    public async Task Solve_Returns_Price_Achieving_Target_Margin()
    {
        var tenant = TenantId.New();
        var product = NewProduct(tenant);
        var handler = NewHandler(tenant, product);

        var result = await handler.Handle(
            new RunIdealPriceSimulatorQuery(product.Id, "MercadoLivre", 15m), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.ComputedMarginPercent.Should().BeApproximately(15m, 0.05m);
        result.Value.SuggestedSalePrice.Currency.Should().Be("BRL");
    }

    [Fact]
    public async Task UnknownMarketplace_ReturnsValidationError()
    {
        var tenant = TenantId.New();
        var product = NewProduct(tenant);
        var handler = NewHandler(tenant, product);

        var result = await handler.Handle(
            new RunIdealPriceSimulatorQuery(product.Id, "Marte", 15m), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Simulator.Marketplace");
    }

    [Fact]
    public async Task UnknownProduct_ReturnsNotFound()
    {
        var tenant = TenantId.New();
        var product = NewProduct(tenant);
        var handler = NewHandler(tenant, product);

        var result = await handler.Handle(
            new RunIdealPriceSimulatorQuery(Guid.NewGuid(), "MercadoLivre", 15m), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }

    [Fact]
    public async Task ForeignTenant_ReturnsNotFound()
    {
        var owner = TenantId.New();
        var product = NewProduct(owner);
        // A different tenant asks for the same product id.
        var handler = NewHandler(TenantId.New(), product);

        var result = await handler.Handle(
            new RunIdealPriceSimulatorQuery(product.Id, "MercadoLivre", 15m), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
