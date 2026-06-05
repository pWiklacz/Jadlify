using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Planning.DailyMacroSummary;

public sealed record GetDailyMacroSummaryQuery(DateOnly Date) : IQuery<DailyMacroSummaryDto>;
