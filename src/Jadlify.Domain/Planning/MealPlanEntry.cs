namespace Jadlify.Domain.Planning;

public sealed class MealPlanEntry
{
    public MealPlanEntry(Guid id, DateOnly date, Guid recipeId, MealType mealType, int portions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(portions);

        Id = id;
        Date = date;
        RecipeId = recipeId;
        MealType = mealType;
        Portions = portions;
    }

    public Guid Id { get; }

    public DateOnly Date { get; private set; }

    public Guid RecipeId { get; private set; }

    public MealType MealType { get; private set; }

    public int Portions { get; private set; }
}
