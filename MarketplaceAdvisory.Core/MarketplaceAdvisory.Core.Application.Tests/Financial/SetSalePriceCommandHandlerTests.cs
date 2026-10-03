using ErrorOr;
using FluentAssertions;
using MarketplaceAdvisory.Core.Application.Financial;
using MarketplaceAdvisory.Core.Application.Financial.Commands.SetSalePrice;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Events;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Tests.Financial;

public sealed class SetSalePriceCommandHandlerTests
{
    private static (SetSalePriceCommandHandler Handler, FakeApplicationDbContext Context, Product Product) Build(
        TenantId tenant, decimal startPrice = 200m)
    {
        var salePrice = new Money(startPrice, "BRL");
        var cmv = new Money(salePrice.Amount * 0.5m, salePrice.Currency);
        var product = Product.Create(
            tenant,
            "SKU-1",
            "Filtro",
            cmv,
            salePrice,
            new Weight(100),
            new Dimensions(10m, 10m, 10m)).Value;
        var context = new FakeApplicationDbContext();

        var handler = new SetSalePriceCommandHandler(
            new FakeProductRepository(product),
            context,
            new FakeProfitabilityInputsAssembler(),
            new ProfitabilityFloorGuard(new ProfitabilityCalculatorService()),
            new StubTenantContext(tenant),
            new ProfitabilityCalculatorService());

        return (handler, context, product);
    }

    [Fact]
    public async Task AboveFloor_Persists_And_Returns_Green()
    {
        var tenant = TenantId.New();
        var (handler, context, product) = Build(tenant);

        var result = await handler.Handle(
            new SetSalePriceCommand(product.Id, "MercadoLivre", 200m, "BRL", Override: null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Status.Should().Be("Green");
        context.SaveChangesCallCount.Should().Be(1);
        product.Price.Amount.Should().Be(200m);
    }

    [Fact]
    public async Task BelowFloor_NoOverride_Returns_FloorOverrideRequired()
    {
        var tenant = TenantId.New();
        var (handler, context, product) = Build(tenant);

        var result = await handler.Handle(
            new SetSalePriceCommand(product.Id, "MercadoLivre", 60m, "BRL", Override: null),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.Conflict);
        result.FirstError.Code.Should().Be("floor.override_required");
        context.SaveChangesCallCount.Should().Be(0);
        product.Price.Amount.Should().Be(200m); // unchanged
    }

    [Fact]
    public async Task BelowFloor_SufficientOverride_Persists_And_Audits()
    {
        var tenant = TenantId.New();
        var (handler, context, product) = Build(tenant);

        // Floor ≈ 94.81; gap from 60 ≈ 34.81, so an accepted loss of 40 is sufficient.
        var result = await handler.Handle(
            new SetSalePriceCommand(product.Id, "MercadoLivre", 60m, "BRL",
                new SalePriceOverride("clearance", "Liquidação", AcceptedLossAmount: 40m)),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        context.SaveChangesCallCount.Should().Be(1);
        product.Price.Amount.Should().Be(60m);
        product.DomainEvents.Should().Contain(e => e is ProfitabilityFloorHit);
    }

    [Fact]
    public async Task BelowFloor_InsufficientOverride_Returns_FloorHit()
    {
        var tenant = TenantId.New();
        var (handler, context, product) = Build(tenant);

        // Accepted loss of 10 is smaller than the ≈34.81 gap → held.
        var result = await handler.Handle(
            new SetSalePriceCommand(product.Id, "MercadoLivre", 60m, "BRL",
                new SalePriceOverride("clearance", null, AcceptedLossAmount: 10m)),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("floor.hit");
        context.SaveChangesCallCount.Should().Be(0);
        product.Price.Amount.Should().Be(200m); // unchanged
    }

    [Fact]
    public async Task UnknownProduct_ReturnsNotFound()
    {
        var tenant = TenantId.New();
        var (handler, _, _) = Build(tenant);

        var result = await handler.Handle(
            new SetSalePriceCommand(Guid.NewGuid(), "MercadoLivre", 100m, "BRL", Override: null),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Type.Should().Be(ErrorType.NotFound);
    }
}
