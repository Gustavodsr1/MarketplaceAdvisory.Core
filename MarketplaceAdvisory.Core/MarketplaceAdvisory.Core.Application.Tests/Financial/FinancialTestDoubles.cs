using ErrorOr;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.Integrations.Abstractions;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using MarketplaceAdvisory.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAdvisory.Core.Application.Tests.Financial;

/// <summary>
/// Deterministic in-memory doubles for the Financial handler tests. They hold a fixed cost
/// structure so tests control the profitability scenario purely through the sale price.
/// </summary>
internal sealed class FakeProfitabilityInputsAssembler : IProfitabilityInputsAssembler
{
    public Money Cmv { get; set; } = new(50m, "BRL");
    public Percentage Commission { get; set; } = new(12m);
    public Money FixedFee { get; set; } = new(5m, "BRL");
    public Money Shipping { get; set; } = new(18m, "BRL");
    public Percentage Tax { get; set; } = new(6m);
    public ProfitabilityThresholds Thresholds { get; set; } =
        new(new Percentage(12m), new Percentage(6m), new Percentage(5m));

    public Task<ErrorOr<ProfitabilityInputs>> AssembleAsync(
        Product product,
        MarketplaceType marketplace,
        Money salePrice,
        CancellationToken cancellationToken)
    {
        var inputs = new ProfitabilityInputs(
            marketplace, Cmv, salePrice, Commission, FixedFee, Shipping, Tax, TaxRegime.SimplesNacional, Thresholds);
        return Task.FromResult<ErrorOr<ProfitabilityInputs>>(inputs);
    }
}

internal sealed class FakeProductRepository : IProductRepository
{
    private readonly Dictionary<Guid, Product> _byId = new();

    public FakeProductRepository(params Product[] seed)
    {
        foreach (var product in seed)
        {
            _byId[product.Id] = product;
        }
    }

    public Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _byId[product.Id] = product;
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult(_byId.GetValueOrDefault(id));
}

/// <summary>
/// Minimal write context that only counts SaveChanges. Handlers load through the repository and
/// never touch the DbSet, so the unsupported getter is never exercised.
/// </summary>
internal sealed class FakeApplicationDbContext : IApplicationDbContext
{
    public int SaveChangesCallCount { get; private set; }

    public DbSet<Product> Products => throw new NotSupportedException("DbSet is not used in handler tests.");

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        SaveChangesCallCount++;
        return Task.FromResult(1);
    }
}

internal sealed class StubTenantContext(TenantId? tenantId) : ITenantContext
{
    public TenantId? TenantId { get; } = tenantId;
}
