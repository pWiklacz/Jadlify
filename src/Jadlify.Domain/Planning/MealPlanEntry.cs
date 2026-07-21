namespace Jadlify.Domain.Planning;

/// <summary>
/// One planned meal on one day. An entry has exactly one source variant: a live recipe
/// reference measured in portions, or an owned product snapshot measured in grams.
/// <see cref="Quantity"/> carries the number and <see cref="Source"/> carries its unit,
/// so a caller can never read grams as portions or vice versa. Use
/// <see cref="RecipePortions"/> / <see cref="ProductGrams"/> when the unit matters.
/// </summary>
public sealed class MealPlanEntry
{
    /// <summary>
    /// Recipe portions are planned in half-portion steps. This is a domain rule, not a UI
    /// convenience: it is enforced here and by the database check constraint.
    /// </summary>
    public const decimal PortionStep = 0.5m;

    private MealPlanEntry()
    {
        // EF Core materialization constructor; populated through mapped members.
    }

    private MealPlanEntry(
        Guid id,
        DateOnly date,
        MealType mealType,
        MealPlanEntrySource source,
        Guid? recipeId,
        PlannedProductSnapshot? product,
        decimal quantity)
    {
        Id = id;
        Date = date;
        MealType = mealType;
        Source = source;
        RecipeId = recipeId;
        Product = product;
        Quantity = quantity;
    }

    public Guid Id { get; }

    public DateOnly Date { get; private set; }

    public MealType MealType { get; private set; }

    public MealPlanEntrySource Source { get; private set; }

    /// <summary>The referenced recipe; non-null exactly when <see cref="Source"/> is Recipe.</summary>
    public Guid? RecipeId { get; private set; }

    /// <summary>The owned product snapshot; non-null exactly when <see cref="Source"/> is Product.</summary>
    public PlannedProductSnapshot? Product { get; private set; }

    /// <summary>
    /// The planned amount, always positive. Its unit follows <see cref="Source"/>: portions
    /// for a recipe entry, grams for a product entry.
    /// </summary>
    public decimal Quantity { get; private set; }

    /// <summary>Portions for a recipe entry; <c>null</c> for a product entry.</summary>
    public decimal? RecipePortions => Source is MealPlanEntrySource.Recipe ? Quantity : null;

    /// <summary>Grams for a product entry; <c>null</c> for a recipe entry.</summary>
    public decimal? ProductGrams => Source is MealPlanEntrySource.Product ? Quantity : null;

    /// <summary>Plans <paramref name="portions"/> of a recipe, in positive multiples of 0.5.</summary>
    public static MealPlanEntry ForRecipe(
        Guid id,
        DateOnly date,
        Guid recipeId,
        MealType mealType,
        decimal portions)
    {
        EnsureValidPortions(portions);

        return new MealPlanEntry(id, date, mealType, MealPlanEntrySource.Recipe, recipeId, null, portions);
    }

    /// <summary>
    /// Plans <paramref name="grams"/> of a single product. Grams are any positive decimal —
    /// the UI's 10 g stepper is a presentation choice, not a hidden domain rule.
    /// </summary>
    public static MealPlanEntry ForProduct(
        Guid id,
        DateOnly date,
        PlannedProductSnapshot product,
        MealType mealType,
        decimal grams)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(grams);

        return new MealPlanEntry(id, date, mealType, MealPlanEntrySource.Product, null, product, grams);
    }

    /// <summary>
    /// Updates the meal type and the planned quantity, validated against this entry's own
    /// source. The identity, date, and source (recipe reference or product snapshot) are
    /// intentionally immutable here: changing those means deleting the entry and adding a new
    /// one, or — from Phase 6 on — moving it through a named behavior.
    /// </summary>
    public void UpdateDetails(MealType mealType, decimal quantity)
    {
        if (Source is MealPlanEntrySource.Recipe)
        {
            EnsureValidPortions(quantity);
        }
        else
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        }

        MealType = mealType;
        Quantity = quantity;
    }

    private static void EnsureValidPortions(decimal portions)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(portions);

        if (portions % PortionStep != 0m)
        {
            throw new ArgumentOutOfRangeException(
                nameof(portions),
                portions,
                $"Recipe portions must be a positive multiple of {PortionStep}.");
        }
    }
}
