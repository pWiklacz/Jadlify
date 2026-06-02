using FluentValidation;

namespace Jadlify.Application.Recipes.GetRecipe;

public sealed class GetRecipeQueryValidator : AbstractValidator<GetRecipeQuery>
{
    public GetRecipeQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}
