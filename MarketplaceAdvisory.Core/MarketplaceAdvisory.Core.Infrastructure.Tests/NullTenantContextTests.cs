using FluentAssertions;
using MarketplaceAdvisory.Core.Infrastructure.Tenancy;

namespace MarketplaceAdvisory.Core.Infrastructure.Tests;

public sealed class NullTenantContextTests
{
    [Fact]
    public void TenantId_IsNull()
    {
        // Arrange
        var tenantContext = new NullTenantContext();

        // Act
        var tenantId = tenantContext.TenantId;

        // Assert
        tenantId.Should().BeNull();
    }
}
