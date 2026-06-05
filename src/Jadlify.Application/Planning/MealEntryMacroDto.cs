using Jadlify.Application.Recipes;

namespace Jadlify.Application.Planning;

public sealed record MealEntryMacroDto(Guid EntryId, RecipeMacroSummaryDto Macros);
