using MarketplaceAdvisory.SharedKernel.Primitives;

namespace MarketplaceAdvisory.SharedKernel.ValueObjects;

/// <summary>
/// Human override credential passed to <see cref="MarketplaceAdvisory.SharedKernel.ValueObjects" />
/// consumers (see IProfitabilityFloorGuard). Records who accepted the loss, why and by how much,
/// so any override is auditable per Principle VIII.
/// </summary>
public sealed class HumanOverrideToken : ValueObject
{
    public HumanOverrideToken(string actorId, string reasonCode, decimal acceptedLossAmount)
    {
        if (string.IsNullOrWhiteSpace(actorId))
        {
            throw new ArgumentException("Actor id is required.", nameof(actorId));
        }

        if (string.IsNullOrWhiteSpace(reasonCode))
        {
            throw new ArgumentException("Reason code is required.", nameof(reasonCode));
        }

        if (acceptedLossAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(acceptedLossAmount), acceptedLossAmount, "Accepted loss must be non-negative.");
        }

        ActorId = actorId;
        ReasonCode = reasonCode;
        AcceptedLossAmount = acceptedLossAmount;
    }

    public string ActorId { get; }

    public string ReasonCode { get; }

    public decimal AcceptedLossAmount { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return ActorId;
        yield return ReasonCode;
        yield return AcceptedLossAmount;
    }
}
