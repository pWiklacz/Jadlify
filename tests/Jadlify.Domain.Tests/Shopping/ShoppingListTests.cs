using Jadlify.Domain.Planning;
using Jadlify.Domain.Shopping;

namespace Jadlify.Domain.Tests.Shopping;

public class ShoppingListTests
{
    private static readonly DateOnly Day1 = new(2026, 6, 6);
    private static readonly DateOnly Day2 = new(2026, 6, 7);
    private static readonly DateTimeOffset Now = new(2026, 6, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_StoresDaysDistinctAndSorted_WithUnboughtItemsAtVersionOne()
    {
        var oatsId = Guid.NewGuid();

        var list = ShoppingList.Create(
            Guid.NewGuid(),
            "Weekend",
            [Day2, Day1, Day1],
            [Item(oatsId, "Oats", 100m)],
            "fp-initial",
            Now);

        Assert.Equal(ShoppingListStatus.Active, list.Status);
        Assert.Equal(1, list.Version);
        Assert.Equal("fp-initial", list.SourceFingerprint);
        Assert.Equal([Day1, Day2], list.SourceDays.Select(day => day.Date));
        ShoppingListItem item = Assert.Single(list.Items);
        Assert.False(item.IsBought);
        Assert.Equal(100m, item.Grams);
    }

    [Fact]
    public void TrySetItemBought_TicksItem_ReturnsFalseForUnknown_AndDoesNotBumpVersion()
    {
        var oatsId = Guid.NewGuid();
        var list = ShoppingList.Create(
            Guid.NewGuid(), "List", [Day1], [Item(oatsId, "Oats", 100m)], "fp", Now);
        Guid itemId = list.Items[0].Id;

        Assert.True(list.TrySetItemBought(itemId, true));
        Assert.True(list.Items[0].IsBought);
        Assert.False(list.TrySetItemBought(Guid.NewGuid(), true));
        // A tick is a per-item edit; sequential ticks must not invalidate one another.
        Assert.Equal(1, list.Version);
    }

    [Fact]
    public void ApplyRefresh_AppliesBoughtRulesPerSection_AndBumpsVersionAndFingerprint()
    {
        var changedId = Guid.NewGuid();
        var sourceOnlyId = Guid.NewGuid();
        var removedId = Guid.NewGuid();
        var addedId = Guid.NewGuid();

        var list = ShoppingList.Create(
            Guid.NewGuid(),
            "List",
            [Day1],
            [
                Item(changedId, "Apples", 100m, Contribution(MealType.Breakfast, "Fruit salad", 100m)),
                Item(sourceOnlyId, "Oats", 200m, Contribution(MealType.Breakfast, "Porridge", 200m)),
                Item(removedId, "Butter", 50m),
            ],
            "fp-1",
            Now);

        // The shopper has ticked the two items that will change quantity vs. only sources.
        foreach (ShoppingListItem item in list.Items.Where(i => i.ProductId != removedId))
        {
            list.TrySetItemBought(item.Id, true);
        }

        list.ApplyRefresh(
            [
                Item(changedId, "Apples", 150m, Contribution(MealType.Breakfast, "Fruit salad", 150m)),
                Item(sourceOnlyId, "Oats", 200m, Contribution(MealType.Lunch, "Overnight oats", 200m)),
                Item(addedId, "Milk", 500m),
            ],
            "fp-2");

        Assert.Equal(2, list.Version);
        Assert.Equal("fp-2", list.SourceFingerprint);

        ShoppingListItem changed = list.Items.Single(i => i.ProductId == changedId);
        Assert.Equal(150m, changed.Grams);
        Assert.False(changed.IsBought); // quantity changed -> unbought

        ShoppingListItem sourceOnly = list.Items.Single(i => i.ProductId == sourceOnlyId);
        Assert.Equal(200m, sourceOnly.Grams);
        Assert.True(sourceOnly.IsBought); // only sources moved -> tick kept
        Assert.Equal(MealType.Lunch, Assert.Single(sourceOnly.Sources).MealType);

        Assert.DoesNotContain(list.Items, i => i.ProductId == removedId);

        ShoppingListItem added = list.Items.Single(i => i.ProductId == addedId);
        Assert.False(added.IsBought);
    }

    [Fact]
    public void Complete_FreezesTheList_AndBumpsVersion()
    {
        var list = ShoppingList.Create(
            Guid.NewGuid(), "List", [Day1], [Item(Guid.NewGuid(), "Oats", 100m)], "fp", Now);

        DateTimeOffset completedAt = Now.AddHours(3);
        list.Complete(completedAt);

        Assert.Equal(ShoppingListStatus.Completed, list.Status);
        Assert.Equal(completedAt, list.CompletedAt);
        Assert.Equal(2, list.Version);
    }

    [Fact]
    public void CompletedList_RejectsFurtherMutation()
    {
        var list = ShoppingList.Create(
            Guid.NewGuid(), "List", [Day1], [Item(Guid.NewGuid(), "Oats", 100m)], "fp", Now);
        Guid itemId = list.Items[0].Id;
        list.Complete(Now.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => list.TrySetItemBought(itemId, true));
        Assert.Throws<InvalidOperationException>(() =>
            list.ApplyRefresh([Item(Guid.NewGuid(), "Milk", 100m)], "fp-2"));
        Assert.Throws<InvalidOperationException>(() => list.Complete(Now.AddHours(2)));
    }

    private static ShoppingProjectionItem Item(
        Guid productId,
        string name,
        decimal grams,
        params ShoppingProjectionContribution[] contributions) =>
        new(
            productId,
            name,
            null,
            grams,
            contributions.Length == 0 ? [Contribution(MealType.Breakfast, name, grams)] : contributions);

    private static ShoppingProjectionContribution Contribution(MealType mealType, string label, decimal grams) =>
        new(Day1, mealType, label, grams);
}
