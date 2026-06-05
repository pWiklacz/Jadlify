using Jadlify.Domain.Nutrition;

namespace Jadlify.Application.Planning;

public sealed record MacroRemainingDto(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static MacroRemainingDto FromGoalAndTotal(MacroNutrients goal, MacroNutrients total)
    {
        ArgumentNullException.ThrowIfNull(goal);
        ArgumentNullException.ThrowIfNull(total);

        return new(
            goal.Calories - total.Calories,
            goal.Protein - total.Protein,
            goal.Fat - total.Fat,
            goal.Carbohydrates - total.Carbohydrates);
    }
}
