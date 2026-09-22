using MarketplaceAdvisory.Core.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

// Out-of-process migration runner. Intended for CI/CD; never migrate at API startup with
// multiple replicas. Connection string comes from MARKETPLACEADVISORY_DB (see the factory).
var factory = new ApplicationDbContextFactory();
await using var dbContext = factory.CreateDbContext(args);

Console.WriteLine("Applying database migrations...");
await dbContext.Database.MigrateAsync();
Console.WriteLine("Database migrations applied successfully.");
