using Jadlify.Application.Planning;
using Jadlify.Application.Recipes;

namespace Jadlify.API.Planning;

/// <summary>
/// Adds one meal to a day. <c>Date</c> is an ISO <c>yyyy-MM-dd</c> string and <c>MealType</c>
/// is one of the names Breakfast, Lunch, Dinner, or Snack.
/// <para>
/// Exactly one source variant must be supplied: <c>RecipeId</c> with <c>Portions</c>, or
/// <c>ProductId</c> with <c>Grams</c>. The units live in separately named fields on purpose —
/// a single generic quantity would leave the unit to be inferred, and half a portion and half
/// a gram are very different meals. Supplying both variants, or neither, is a 400.
/// </para>
/// </summary>
public sealed record AddMealPlanEntryRequest(
    DateOnly Date,
    string MealType,
    Guid? RecipeId = null,
    decimal? Portions = null,
    Guid? ProductId = null,
    decimal? Grams = null);

/// <summary>
/// Updates the meal type and quantity of an existing entry. The quantity must be supplied in
/// the unit matching the entry's own source: <c>Portions</c> for a recipe entry, <c>Grams</c>
/// for a product entry. The date and the source itself are intentionally not editable here.
/// </summary>
public sealed record UpdateMealPlanEntryRequest(
    string MealType,
    decimal? Portions = null,
    decimal? Grams = null);

public sealed record CreatedMealPlanEntryResponse(Guid Id);

/// <summary>
/// Reschedules an existing entry. The entry keeps its id, source, and quantity — only the day
/// and the meal type change, so anything already referring to the entry still refers to it.
/// </summary>
public sealed record MoveMealPlanEntryRequest(DateOnly Date, string MealType);

/// <summary>
/// Copies an entry onto every date in <c>TargetDates</c>, leaving the original in place. Dates
/// must be distinct; the entry's own date is allowed, which is how a meal is duplicated within
/// a day.
/// </summary>
public sealed record CopyMealPlanEntryRequest(IReadOnlyList<DateOnly> TargetDates);

/// <summary>
/// Copies a whole day onto every date in <c>TargetDates</c>. <c>Mode</c> is <c>Add</c> to keep
/// what the target day already holds, or <c>Replace</c> to leave only the copies. The source
/// date must not appear among the targets, and an empty source day is a 400 rather than a
/// silent way to clear days.
/// </summary>
public sealed record CopyMealPlanDayRequest(IReadOnlyList<DateOnly> TargetDates, string Mode);

/// <summary>An entry a copy created: the new id and the day it landed on.</summary>
public sealed record CopiedMealPlanEntryResponse(Guid Id, DateOnly Date, string MealType)
{
    public static CopiedMealPlanEntryResponse FromDto(CreatedMealPlanEntryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new CopiedMealPlanEntryResponse(dto.Id, dto.Date, dto.MealType.ToString());
    }
}

/// <summary>
/// Everything a copy operation created, in target-date order. The whole batch is written or
/// none of it is, so a success here means every listed entry exists.
/// </summary>
public sealed record CopiedMealPlanEntriesResponse(IReadOnlyList<CopiedMealPlanEntryResponse> Entries)
{
    public static CopiedMealPlanEntriesResponse FromDtos(IReadOnlyList<CreatedMealPlanEntryDto> dtos)
    {
        ArgumentNullException.ThrowIfNull(dtos);

        return new CopiedMealPlanEntriesResponse([.. dtos.Select(CopiedMealPlanEntryResponse.FromDto)]);
    }
}

/// <summary>
/// A planned meal. <c>Source</c> is <c>Recipe</c> or <c>Product</c> and says which group of
/// fields is populated: a recipe entry carries <c>RecipeId</c>/<c>RecipeName</c>/<c>Portions</c>
/// with the product fields null, a product entry the reverse.
/// <para>
/// A recipe entry's name is resolved from the live recipe, so renaming the recipe renames it
/// here. A product entry's name and category come from the snapshot taken when it was planned
/// and do not follow later catalog edits or deletion.
/// </para>
/// </summary>
public sealed record MealPlanEntryResponse(
    Guid Id,
    DateOnly Date,
    string MealType,
    string Source,
    Guid? RecipeId,
    string? RecipeName,
    decimal? Portions,
    Guid? ProductId,
    string? ProductName,
    string? Category,
    decimal? Grams)
{
    public static MealPlanEntryResponse FromDto(MealPlanEntryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new MealPlanEntryResponse(
            dto.Id,
            dto.Date,
            dto.MealType.ToString(),
            dto.Source.ToString(),
            dto.RecipeId,
            dto.RecipeName,
            dto.Portions,
            dto.ProductId,
            dto.ProductName,
            dto.Category?.ToString(),
            dto.Grams);
    }
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
    public static MealEntryMacroResponse FromDto(MealEntryMacroDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new MealEntryMacroResponse(dto.EntryId, MacroSummaryResponse.FromDto(dto.Macros));
    }
}

public sealed record DailyMacroSummaryResponse(
    DateOnly Date,
    IReadOnlyList<MealEntryMacroResponse> Entries,
    MacroSummaryResponse Total,
    MacroSummaryResponse? Goal,
    MacroSummaryResponse? Remaining)
{
    public static DailyMacroSummaryResponse FromDto(DailyMacroSummaryDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new DailyMacroSummaryResponse(
            dto.Date,
            [.. dto.Entries.Select(MealEntryMacroResponse.FromDto)],
            MacroSummaryResponse.FromDto(dto.Total),
            dto.Goal is null ? null : MacroSummaryResponse.FromDto(dto.Goal),
            dto.Remaining is null ? null : MacroSummaryResponse.FromDto(dto.Remaining));
    }
}

/// <summary>
/// One day of a planner range. <c>Remaining</c> is signed: negative once the day is over its
/// goal. <c>Goal</c> and <c>Remaining</c> are null together when no goal is configured.
/// </summary>
public sealed record MealPlanDayResponse(
    DateOnly Date,
    IReadOnlyList<MealPlanEntryResponse> Entries,
    IReadOnlyList<MealEntryMacroResponse> EntryMacros,
    MacroSummaryResponse Total,
    MacroSummaryResponse? Goal,
    MacroSummaryResponse? Remaining)
{
    public static MealPlanDayResponse FromDto(MealPlanDayDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new MealPlanDayResponse(
            dto.Date,
            [.. dto.Entries.Select(MealPlanEntryResponse.FromDto)],
            [.. dto.EntryMacros.Select(MealEntryMacroResponse.FromDto)],
            MacroSummaryResponse.FromDto(dto.Total),
            dto.Goal is null ? null : MacroSummaryResponse.FromDto(dto.Goal),
            dto.Remaining is null ? null : MacroSummaryResponse.FromDto(dto.Remaining));
    }
}

/// <summary>
/// An inclusive window of planned days. Every date from <c>From</c> to <c>To</c> is present in
/// <c>Days</c> in ascending order, empty days included, so the caller renders a grid without
/// filling gaps itself.
/// </summary>
public sealed record MealPlanRangeResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyList<MealPlanDayResponse> Days)
{
    public static MealPlanRangeResponse FromDto(MealPlanRangeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new MealPlanRangeResponse(
            dto.From,
            dto.To,
            [.. dto.Days.Select(MealPlanDayResponse.FromDto)]);
    }
}
