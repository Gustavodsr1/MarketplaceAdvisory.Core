using MarketplaceAdvisory.Core.Application.Common.Abstractions;
using MarketplaceAdvisory.Core.Domain.Catalog;
using MarketplaceAdvisory.Core.Infrastructure.BackgroundJobs.Jobs;
using MarketplaceAdvisory.Core.Infrastructure.Cache;
using MarketplaceAdvisory.Core.Infrastructure.Integrations;
using MarketplaceAdvisory.Core.Infrastructure.Persistence;
using MarketplaceAdvisory.Core.Infrastructure.Persistence.Repositories;
using MarketplaceAdvisory.Core.Infrastructure.Tenancy;
using MarketplaceAdvisory.Integrations.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;

namespace MarketplaceAdvisory.Core.Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: EF Core (writes), Dapper (reads), Redis cache,
/// the marketplace anti-corruption layer and Quartz background jobs.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        // Default tenant context. The API layer overrides this with an HTTP-aware implementation.
        services.AddScoped<ITenantContext, NullTenantContext>();

        // Write model (commands) via EF Core + Npgsql.
        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(postgresConnectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        services.AddScoped<IProductRepository, ProductRepository>();

        // Read model (queries) via Dapper.
        services.AddScoped<IReadDbConnection>(_ => new ReadDbConnection(postgresConnectionString));

        // Distributed cache via Redis.
        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        services.AddSingleton<ICacheService, RedisCacheService>();

        // Marketplace anti-corruption layer: one adapter per marketplace + a resolving factory.
        services.AddScoped<IMarketplaceAdapter, MercadoLivreAdapter>();
        services.AddScoped<IMarketplaceAdapter, ShopeeAdapter>();
        services.AddScoped<IMarketplaceAdapter, AmazonAdapter>();
        services.AddScoped<IMarketplaceAdapterFactory, MarketplaceAdapterFactory>();

        // Background jobs (Quartz.NET) with a placeholder hourly synchronization trigger.
        services.AddQuartz(quartz =>
        {
            var jobKey = new JobKey(nameof(MarketplaceSyncJob));

            quartz.AddJob<MarketplaceSyncJob>(job => job.WithIdentity(jobKey));
            quartz.AddTrigger(trigger => trigger
                .ForJob(jobKey)
                .WithIdentity($"{nameof(MarketplaceSyncJob)}-trigger")
                .WithCronSchedule("0 0 * * * ?"));
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        return services;
    }
}
