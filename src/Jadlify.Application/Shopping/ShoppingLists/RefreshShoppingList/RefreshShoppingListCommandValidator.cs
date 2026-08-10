using FluentValidation;

namespace Jadlify.Application.Shopping.ShoppingLists.RefreshShoppingList;

public sealed class RefreshShoppingListCommandValidator : AbstractValidator<RefreshShoppingListCommand>
{
    public RefreshShoppingListCommandValidator()
    {
        // The fingerprint identifies the exact projection the user previewed; without it there
        // is nothing to reconcile the apply against, so an empty value is a client error.
        RuleFor(x => x.ExpectedSourceFingerprint)
            .NotEmpty()
            .WithMessage("A source fingerprint from the preview is required to apply a refresh.");
    }
}
