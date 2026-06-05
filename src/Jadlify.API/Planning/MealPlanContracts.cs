using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;

namespace Jadlify.API.Planning;

/// <summary>
/// Adds a recipe to a selected day and meal type. <c>Date</c> is an ISO <c>yyyy-MM-dd</c>
/// string and <c>MealType</c> is one of the names Breakfast, Lunch, Dinner, or Snack.
/// </summary>
public sealed record AddMealPlanEntryRequest(
    DateOnly Date,
    Guid RecipeId,
    string MealType,
    int Portions);

/// <summary>
/// Updates only the meal type and portions of an existing entry; the date and referenced
/// recipe are intentionally not editable from the API surface.
/// </summary>
public sealed record UpdateMealPlanEntryRequest(
    string MealType,
    int Portions);

public sealed record CreatedMealPlanEntryResponse(Guid Id);

/// <summary>
/// A meal-plan entry on the selected day. <c>MealType</c> is surfaced as its name and the
/// recipe name is resolved from the current owner-scoped recipe; S-04 stops at display data,
/// so no day-level macro totals are included.
/// </summary>
public sealed record MealPlanEntryResponse(
    Guid Id,
    DateOnly Date,
    Guid RecipeId,
    string RecipeName,
    string MealType,
    int Portions)
{
    public static MealPlanEntryResponse FromDto(MealPlanEntryDto dto) =>
        new(
            dto.Id,
            dto.Date,
            dto.RecipeId,
            dto.RecipeName,
            dto.MealType.ToString(),
            dto.Portions);
}

public sealed record MacroSummaryResponse(
    decimal Calories,
    decimal Protein,
    decimal Fat,
    decimal Carbohydrates)
{
    public static MacroSummaryResponse FromDto(RecipeMacroSummaryDto dto) =>
        new(dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);

    public static MacroSummaryResponse FromDto(PlanningMacroGoalDto dto) =>
        new(dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);

    public static MacroSummaryResponse FromDto(MacroRemainingDto dto) =>
        new(dto.Calories, dto.Protein, dto.Fat, dto.Carbohydrates);
}

public sealed record MealEntryMacroResponse(
    Guid EntryId,
    MacroSummaryResponse Macros)
{
    public static MealEntryMacroResponse FromDto(MealEntryMacroDto dto) =>
        new(dto.EntryId, MacroSummaryResponse.FromDto(dto.Macros));
}

public sealed record DailyMacroSummaryResponse(
    DateOnly Date,
    IReadOnlyList<MealEntryMacroResponse> Entries,
    MacroSummaryResponse Total,
    MacroSummaryResponse? Goal,
    MacroSummaryResponse? Remaining)
{
    public static DailyMacroSummaryResponse FromDto(DailyMacroSummaryDto dto) =>
        new(
            dto.Date,
            dto.Entries.Select(MealEntryMacroResponse.FromDto).ToArray(),
            MacroSummaryResponse.FromDto(dto.Total),
            dto.Goal is null ? null : MacroSummaryResponse.FromDto(dto.Goal),
            dto.Remaining is null ? null : MacroSummaryResponse.FromDto(dto.Remaining));
}
