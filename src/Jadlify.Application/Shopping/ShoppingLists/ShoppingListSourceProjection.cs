using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.Domain.Shopping;

namespace Jadlify.Application.Shopping.ShoppingLists;

/// <summary>A projection of a shopping list's source days plus the fingerprint of that projection.</summary>
public sealed record ShoppingSourceProjection(
    IReadOnlyList<ShoppingProjectionItem> Items,
    string Fingerprint);

/// <summary>
/// Builds the shopping projection for an arbitrary set of source days in one pass, shared by
/// create, diff-preview, and refresh so all three see identical items and fingerprints. Reads
/// the plan once over the bounding window and filters to the selected days, then batches the
/// distinct recipes those meals reference — never a query per day or per recipe.
/// </summary>
public static class ShoppingListSourceProjection
{
    public static async Task<ShoppingSourceProjection> BuildAsync(
        IMealPlanRepository mealPlans,
        IRecipeRepository recipes,
        IReadOnlyCollection<DateOnly> days,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(mealPlans);
        ArgumentNullException.ThrowIfNull(recipes);
        ArgumentNullException.ThrowIfNull(days);

        HashSet<DateOnly> selectedDays = [.. days];
        if (selectedDays.Count == 0)
        {
            IReadOnlyList<ShoppingProjectionItem> emptyProjection = ShoppingListCalculator.ForMealEntries([]);
            return new ShoppingSourceProjection(emptyProjection, ShoppingListCalculator.ComputeFingerprint(emptyProjection));
        }

        // One owner-scoped read over the bounding window, then filtered to the chosen days: a
        // sparse selection across a month still costs a single query, not one per day.
        DateOnly from = selectedDays.Min();
        DateOnly to = selectedDays.Max();
        IReadOnlyList<MealPlanEntry> entries =
            (await mealPlans.ListByDateRangeAsync(from, to, cancellationToken))
            .Where(entry => selectedDays.Contains(entry.Date))
            .ToList();

        Guid[] recipeIds = MealPlanRecipeResolution.DistinctRecipeIds(entries);
        IReadOnlyList<Recipe> loadedRecipes = recipeIds.Length == 0
            ? []
            : await recipes.ListByIdsWithIngredientsAsync(recipeIds, cancellationToken);
        var recipesById = loadedRecipes.ToDictionary(recipe => recipe.Id);

        List<(MealPlanEntry Entry, Recipe? Recipe)> pairs = [];
        foreach (MealPlanEntry entry in entries)
        {
            // A recipe entry whose recipe cannot be resolved contributes nothing rather than
            // failing the whole list; recipe deletion stays blocked while a recipe is planned,
            // so this only guards a genuinely missing reference.
            if (MealPlanRecipeResolution.TryResolve(entry, recipesById, out Recipe? recipe))
            {
                pairs.Add((entry, recipe));
            }
        }

        IReadOnlyList<ShoppingProjectionItem> projection = ShoppingListCalculator.ForMealEntries(pairs);
        return new ShoppingSourceProjection(projection, ShoppingListCalculator.ComputeFingerprint(projection));
    }
}
