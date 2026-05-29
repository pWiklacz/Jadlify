namespace Jadlify.Domain.Nutrition;

public sealed record MacroNutrients
{
    public MacroNutrients(decimal calories, decimal protein, decimal fat, decimal carbohydrates)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(calories);
        ArgumentOutOfRangeException.ThrowIfNegative(protein);
        ArgumentOutOfRangeException.ThrowIfNegative(fat);
        ArgumentOutOfRangeException.ThrowIfNegative(carbohydrates);

        Calories = calories;
        Protein = protein;
        Fat = fat;
        Carbohydrates = carbohydrates;
    }

    public static MacroNutrients Zero { get; } = new(0m, 0m, 0m, 0m);

    public decimal Calories { get; }

    public decimal Protein { get; }

    public decimal Fat { get; }

    public decimal Carbohydrates { get; }

    public MacroNutrients Scale(decimal factor) =>
        new(Calories * factor, Protein * factor, Fat * factor, Carbohydrates * factor);

    public static MacroNutrients operator +(MacroNutrients left, MacroNutrients right) =>
        new(
            left.Calories + right.Calories,
            left.Protein + right.Protein,
            left.Fat + right.Fat,
            left.Carbohydrates + right.Carbohydrates);
}
