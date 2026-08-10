using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Planning;

/// <summary>
/// Helpers for pairing planned meals with the live recipes they reference. Product entries
/// carry their own snapshot and need no lookup, so they are skipped when collecting ids and
/// pair with a <c>null</c> recipe — the shape the shared macro and shopping cores expect.
/// </summary>
internal static class MealPlanRecipeResolution
{
    /// <summary>
    /// The recipe ids a batch of entries references, deduplicated. Feeding this to one
    /// owner-scoped batch read is what keeps a range query off a per-entry lookup.
    /// </summary>
    public static Guid[] DistinctRecipeIds(IEnumerable<MealPlanEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);

        return entries
            .Where(entry => entry.Source is MealPlanEntrySource.Recipe)
            .Select(entry => entry.RecipeId!.Value)
            .Distinct()
            .ToArray();
    }

    /// <summary>
    /// Resolves the recipe an entry needs for calculation. Returns <see langword="true"/> with
    /// a <c>null</c> recipe for a product entry, and <see langword="false"/> when a recipe
    /// entry points at a recipe the batch did not return — the caller decides whether that is
    /// a skipped contribution or a surfaced warning.
    /// </summary>
    public static bool TryResolve(
        MealPlanEntry entry,
        IReadOnlyDictionary<Guid, Recipe> recipesById,
        out Recipe? recipe)
    {
        ArgumentNullException.ThrowIfNull(entry);
        ArgumentNullException.ThrowIfNull(recipesById);

        if (entry.Source is MealPlanEntrySource.Product)
        {
            recipe = null;
            return true;
        }

        bool found = recipesById.TryGetValue(entry.RecipeId!.Value, out Recipe? resolved);
        recipe = resolved;
        return found;
    }
}
