using FluentValidation;

namespace MarketplaceAdvisory.Core.Application.Financial.Commands.SetSalePrice;

public sealed class SetSalePriceCommandValidator : AbstractValidator<SetSalePriceCommand>
{
    public SetSalePriceCommandValidator()
    {
        RuleFor(command => command.ProductId).NotEmpty();
        RuleFor(command => command.Marketplace).NotEmpty();
        RuleFor(command => command.Amount).GreaterThan(0m);
        RuleFor(command => command.Currency).NotEmpty().Length(3);

        When(command => command.Override is not null, () =>
        {
            RuleFor(command => command.Override!.ReasonCode).NotEmpty();
            RuleFor(command => command.Override!.AcceptedLossAmount).GreaterThanOrEqualTo(0m);
        });
    }
}
