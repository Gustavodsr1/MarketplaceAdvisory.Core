using FluentValidation;

namespace MarketplaceAdvisory.Core.Application.Financial.Queries.RunIdealPriceSimulator;

public sealed class RunIdealPriceSimulatorQueryValidator : AbstractValidator<RunIdealPriceSimulatorQuery>
{
    public RunIdealPriceSimulatorQueryValidator()
    {
        RuleFor(query => query.ProductId).NotEmpty();
        RuleFor(query => query.Marketplace).NotEmpty();
        RuleFor(query => query.TargetNetMarginPercent)
            .GreaterThan(0m)
            .LessThan(100m)
            .WithMessage("Target net margin must be between 0 and 100 (exclusive).");
    }
}
