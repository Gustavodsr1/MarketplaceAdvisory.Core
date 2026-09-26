using MarketplaceAdvisory.Core.Domain.Financial;
using MarketplaceAdvisory.SharedKernel.ValueObjects;

namespace MarketplaceAdvisory.Core.Application.Common.Abstractions;

/// <summary>
/// Principle VIII enforcement point. Every price-changing command MUST consult this guard
/// before writing. Concrete implementations live in the Application layer so they can access
/// tenant configuration and the Financial Engine.
/// </summary>
public interface IProfitabilityFloorGuard
{
    /// <summary>
    /// Evaluates whether a proposed sale price passes the tenant's minimum-margin floor for
    /// the given profitability inputs.
    /// </summary>
    /// <param name="inputs">
    /// Fully-populated profitability inputs. Note that the caller MUST set
    /// <see cref="ProfitabilityInputs.SalePrice"/> to the *proposed* price under evaluation.
    /// </param>
    /// <param name="overrideToken">Optional human override (Principle VIII escape hatch).</param>
    Task<FloorDecision> EvaluateAsync(
        ProfitabilityInputs inputs,
        HumanOverrideToken? overrideToken,
        CancellationToken cancellationToken);
}
