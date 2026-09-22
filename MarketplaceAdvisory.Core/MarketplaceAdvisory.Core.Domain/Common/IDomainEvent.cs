namespace MarketplaceAdvisory.Core.Domain.Common;

/// <summary>
/// Marker for domain events raised by aggregates and dispatched after persistence.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}
