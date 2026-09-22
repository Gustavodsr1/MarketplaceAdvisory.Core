using Microsoft.Extensions.Logging;
using Quartz;

namespace MarketplaceAdvisory.Core.Infrastructure.BackgroundJobs.Jobs;

/// <summary>
/// Placeholder recurring job that will synchronize orders/listings from the marketplaces.
/// Wrapped by Quartz so it gets retries, scheduling and distributed coordination out of the box.
/// </summary>
[DisallowConcurrentExecution]
internal sealed class MarketplaceSyncJob(ILogger<MarketplaceSyncJob> logger) : IJob
{
    public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "MarketplaceSyncJob placeholder executed at {ExecutedAt:o}.",
            DateTimeOffset.UtcNow);

        return ValueTask.CompletedTask;
    }
}
