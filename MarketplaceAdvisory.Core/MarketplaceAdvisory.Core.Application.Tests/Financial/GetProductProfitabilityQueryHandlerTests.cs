using FluentAssertions;
using MarketplaceAdvisory.Core.Application.Financial.Queries.GetProductProfitability;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Tests.Financial;

public sealed class GetProductProfitabilityQueryHandlerTests
{
    [Fact]
    public async Task Returns_Yellow_For_SpecScenario()
    {
        var tenant = TenantId.New();
        var product = Product.Create(tenant, "SKU-1", "Filtro", new Money(100m, "BRL")).Value;

        var handler = new GetProductProfitabilityQueryHandler(
            new FakeProductRepository(product),
            new FakeProfitabilityInputsAssembler(),
            new StubTenantContext(tenant),
            new ProfitabilityCalculatorService());

        var result = await handler.Handle(
            new GetProductProfitabilityQuery(product.Id), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Should().ContainSingle();

        var dto = result.Value[0];
        dto.Marketplace.Should().Be("MercadoLivre");
        dto.NetProfit.Amount.Should().Be(9m);
        dto.NetMarginPercent.Should().Be(9m);
        dto.Status.Should().Be("Yellow");
    }
}
