using Jadlify.Domain.Nutrition;

namespace Jadlify.Application.Recipes;

public sealed record RecipeMacroSummaryDto(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static RecipeMacroSummaryDto FromDomain(MacroNutrients macros) =>
        new(macros.Calories, macros.Protein, macros.Fat, macros.Carbohydrates);
}
