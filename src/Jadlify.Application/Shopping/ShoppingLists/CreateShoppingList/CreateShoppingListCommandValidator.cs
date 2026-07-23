using FluentValidation;

namespace Jadlify.Application.Shopping.ShoppingLists.CreateShoppingList;

public sealed class CreateShoppingListCommandValidator : AbstractValidator<CreateShoppingListCommand>
{
    public CreateShoppingListCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Give the shopping list a name.")
            .MaximumLength(200)
            .WithMessage("The name must be at most 200 characters.");

        RuleFor(x => x.Days)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage(ShoppingListDayRules.DaysRequiredMessage)
            .Must(ShoppingListDayRules.IsWithinCount)
            .WithMessage(ShoppingListDayRules.DaysCountMessage)
            .Must(ShoppingListDayRules.AreDistinct)
            .WithMessage(ShoppingListDayRules.DaysDistinctMessage)
            .Must(ShoppingListDayRules.IsWithinSpan)
            .WithMessage(ShoppingListDayRules.DaysSpanMessage);
    }
}
