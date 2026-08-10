using Jadlify.Domain.Planning;
using Jadlify.Domain.Shopping;

namespace Jadlify.Domain.Tests.Shopping;

public class ShoppingListDiffTests
{
    private static readonly DateOnly Day = new(2026, 6, 6);

    [Fact]
    public void Between_ClassifiesAddedRemovedAndChanged()
    {
        var keptId = Guid.NewGuid();
        var droppedId = Guid.NewGuid();
        var addedId = Guid.NewGuid();

        IReadOnlyList<ShoppingListItem> stored =
        [
            StoredItem(keptId, "Apples", 100m),
            StoredItem(droppedId, "Bananas", 200m),
        ];
        IReadOnlyList<ShoppingProjectionItem> projection =
        [
            Item(keptId, "Apples", 150m),
            Item(addedId, "Carrots", 50m),
        ];

        var diff = ShoppingListDiff.Between(stored, projection);

        Assert.True(diff.HasChanges);
        Assert.Equal(addedId, Assert.Single(diff.Added).ProductId);
        Assert.Equal(droppedId, Assert.Single(diff.Removed).ProductId);
        ShoppingListDiffLine changed = Assert.Single(diff.Changed);
        Assert.Equal(keptId, changed.ProductId);
        Assert.Equal(100m, changed.PreviousGrams);
        Assert.Equal(150m, changed.NewGrams);
        Assert.Empty(diff.SourceOnly);
    }

    [Fact]
    public void Between_FlagsSourceOnly_WhenTotalEqualButContributionsDiffer()
    {
        var productId = Guid.NewGuid();

        IReadOnlyList<ShoppingListItem> stored =
        [
            StoredItem(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m)),
        ];
        IReadOnlyList<ShoppingProjectionItem> projection =
        [
            Item(productId, "Oats", 100m, Contribution(MealType.Lunch, "Overnight oats", 100m)),
        ];

        var diff = ShoppingListDiff.Between(stored, projection);

        Assert.True(diff.HasChanges);
        Assert.Empty(diff.Changed);
        Assert.Equal(productId, Assert.Single(diff.SourceOnly).ProductId);
    }

    [Fact]
    public void Between_ReportsNoChanges_WhenTotalsAndSourcesMatch()
    {
        var productId = Guid.NewGuid();

        IReadOnlyList<ShoppingListItem> stored =
        [
            StoredItem(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m)),
        ];
        IReadOnlyList<ShoppingProjectionItem> projection =
        [
            Item(productId, "Oats", 100m, Contribution(MealType.Breakfast, "Porridge", 100m)),
        ];

        var diff = ShoppingListDiff.Between(stored, projection);

        Assert.False(diff.HasChanges);
        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Empty(diff.Changed);
        Assert.Empty(diff.SourceOnly);
    }

    private static ShoppingListItem StoredItem(
        Guid productId,
        string name,
        decimal grams,
        params ShoppingProjectionContribution[] contributions) =>
        ShoppingListItem.FromProjection(Item(productId, name, grams, contributions));

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
        new(Day, mealType, label, grams);
}
