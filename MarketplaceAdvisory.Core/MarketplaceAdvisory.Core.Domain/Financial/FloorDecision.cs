using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Domain.Financial;

/// <summary>
/// Discriminated-union result returned by <c>IProfitabilityFloorGuard</c>. Every price-changing
/// command MUST consult the guard and honor its verdict (Principle VIII).
/// </summary>
public abstract record FloorDecision
{
    private FloorDecision() { }

    /// <summary>The write may proceed at <see cref="Price"/> — margin is at or above the floor.</summary>
    public sealed record Allowed(Money Price) : FloorDecision;

    /// <summary>
    /// The write must be blocked — proposed price would violate the tenant's minimum floor.
    /// A <see cref="Events.ProfitabilityFloorHit"/> event MUST be raised by the caller.
    /// </summary>
    public sealed record HeldFloorHit(Money Floor, Money Proposed) : FloorDecision;

    /// <summary>
    /// The write is above the floor but the caller supplied an explicit <c>HumanOverrideToken</c>.
    /// The write may proceed only if the token is valid; the caller is responsible for the audit trail.
    /// </summary>
    public sealed record RequiresOverride(Money Floor, Money Proposed, decimal Diff) : FloorDecision;
}
