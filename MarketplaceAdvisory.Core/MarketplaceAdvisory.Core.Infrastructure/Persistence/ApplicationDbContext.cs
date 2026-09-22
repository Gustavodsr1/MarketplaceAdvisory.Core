using System.Reflection;
using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.SharedKernel.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace MarketplaceAdvisory.Core.Infrastructure.Persistence;

/// <summary>
/// EF Core write-side database context. Applies entity configurations and enforces
/// multi-tenant isolation through a global query filter based on the current tenant.
/// </summary>
public sealed class ApplicationDbContext : DbContext, IApplicationDbContext
{
    private readonly TenantId? _tenantId;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    public DbSet<Product> Products => Set<Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("catalog");
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

        modelBuilder.Entity<Product>()
            .HasQueryFilter(product => !_tenantId.HasValue || product.TenantId == _tenantId.Value);

        base.OnModelCreating(modelBuilder);
    }
}
