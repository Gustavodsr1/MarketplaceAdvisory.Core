using MarketplaceAdvisory.Core.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MarketplaceAdvisory.Core.Infrastructure.Persistence;

/// <summary>
/// Design-time factory so `dotnet ef migrations add` works without booting the API.
/// Reads the connection string from the MARKETPLACEADVISORY_DB environment variable,
/// falling back to the local Docker Compose defaults.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("MARKETPLACEADVISORY_DB")
            ?? "Host=localhost;Port=5432;Database=marketplaceadvisory;Username=marketplaceadvisory;Password=local_dev_password";

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options, new NullTenantContext());
    }
}
