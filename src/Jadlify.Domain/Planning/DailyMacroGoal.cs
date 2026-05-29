using Jadlify.Domain.Nutrition;

namespace Jadlify.Domain.Planning;

public sealed class DailyMacroGoal
{
    private DailyMacroGoal()
    {
        // EF Core materialization constructor; populated through mapped members.
        Target = null!;
    }

    public DailyMacroGoal(Guid id, MacroNutrients target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Id = id;
        Target = target;
    }

    public Guid Id { get; }

    public MacroNutrients Target { get; private set; }

    public void UpdateTarget(MacroNutrients target)
    {
        ArgumentNullException.ThrowIfNull(target);

        Target = target;
    }
}
