using Jadlify.Application.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;

namespace Jadlify.Application.Planning;

/// <summary>
/// Turns one day's planned meals into per-entry macros plus the day total, from an
/// already-loaded recipe batch. Both the single-day summary and the range query project
/// through here so a day means the same thing in a day view and in a month grid.
/// </summary>
internal static class MealPlanDayProjection
{
    /// <summary>
    /// Projects <paramref name="entries"/> against <paramref name="recipesById"/>. A recipe
    /// entry whose recipe is absent — deleted, or simply not in this batch — contributes zero
    /// rather than throwing, matching how a partially resolvable day has always been read.
    /// </summary>
    public static (IReadOnlyList<MealEntryMacroDto> EntryMacros, MacroNutrients Total) Build(
        IReadOnlyList<MealPlanEntry> entries,
        IReadOnlyDictionary<Guid, Recipe> recipesById)
    {
        ArgumentNullException.ThrowIfNull(entries);
        ArgumentNullException.ThrowIfNull(recipesById);

        List<MealEntryMacroDto> entryMacros = new(entries.Count);
        List<(MealPlanEntry Entry, Recipe? Recipe)> resolved = new(entries.Count);

        foreach (MealPlanEntry entry in entries)
        {
            if (!MealPlanRecipeResolution.TryResolve(entry, recipesById, out Recipe? recipe))
            {
                entryMacros.Add(new MealEntryMacroDto(
                    entry.Id,
                    RecipeMacroSummaryDto.FromDomain(MacroNutrients.Zero)));
                continue;
            }

            resolved.Add((entry, recipe));
            entryMacros.Add(new MealEntryMacroDto(
                entry.Id,
                RecipeMacroSummaryDto.FromDomain(MacroCalculator.ForMealEntry(entry, recipe))));
        }

        return (entryMacros, MacroCalculator.DayTotal(resolved));
    }
}
