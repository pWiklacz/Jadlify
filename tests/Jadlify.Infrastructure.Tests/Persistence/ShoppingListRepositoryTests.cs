using Jadlify.Application.Identity;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;
using Jadlify.Domain.Shopping;
using Jadlify.Infrastructure.Persistence;
using Jadlify.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Tests.Persistence;

public class ShoppingListRepositoryTests
{
    private static readonly ApplicationUserId OwnerId = new("user-owner");
    private static readonly ApplicationUserId OtherId = new("user-other");
    private static readonly DateOnly Day1 = new(2026, 6, 6);
    private static readonly DateOnly Day2 = new(2026, 6, 7);
    private static readonly DateTimeOffset Now = new(2026, 6, 6, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AddAsync_RoundTripsTheWholeAggregate()
    {
        using SqliteTestDatabase database = new();
        var productId = Guid.NewGuid();
        var listId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            var list = ShoppingList.Create(
                listId,
                "Weekend",
                [Day2, Day1],
                [Item(productId, "Oats", 100m, ProductCategory.GrainsAndBread, Source(MealType.Breakfast, "Porridge", 100m))],
                "fp-1",
                Now);
            await lists.AddAsync(list);
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            ShoppingList? reloaded = await lists.GetByIdAsync(listId);

            Assert.NotNull(reloaded);
            Assert.Equal("Weekend", reloaded!.Name);
            Assert.Equal(ShoppingListStatus.Active, reloaded.Status);
            Assert.Equal("fp-1", reloaded.SourceFingerprint);
            Assert.Equal(1, reloaded.Version);
            Assert.Equal([Day1, Day2], reloaded.SourceDays.Select(day => day.Date));

            ShoppingListItem item = Assert.Single(reloaded.Items);
            Assert.Equal(productId, item.ProductId);
            Assert.Equal("Oats", item.ProductName);
            Assert.Equal(ProductCategory.GrainsAndBread, item.Category);
            Assert.Equal(100m, item.Grams);
            Assert.False(item.IsBought);

            ShoppingListItemSource source = Assert.Single(item.Sources);
            Assert.Equal(MealType.Breakfast, source.MealType);
            Assert.Equal("Porridge", source.SourceLabel);
            Assert.Equal(100m, source.Grams);
        }
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersList()
    {
        using SqliteTestDatabase database = new();
        var listId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            await lists.AddAsync(Build(listId));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OtherId));
            Assert.Null(await lists.GetByIdAsync(listId));
        }
    }

    [Fact]
    public async Task GetActiveAsync_ReturnsOnlyCurrentUsersActiveList()
    {
        using SqliteTestDatabase database = new();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository ownerLists = new(context, new TestCurrentUser(OwnerId));
            await ownerLists.AddAsync(Build(Guid.NewGuid(), "Owner list"));
            ShoppingListRepository otherLists = new(context, new TestCurrentUser(OtherId));
            await otherLists.AddAsync(Build(Guid.NewGuid(), "Other list"));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            ShoppingList? active = await lists.GetActiveAsync();

            Assert.NotNull(active);
            Assert.Equal("Owner list", active!.Name);
        }
    }

    [Fact]
    public async Task AddAsync_RejectsASecondActiveListForTheSameUser()
    {
        using SqliteTestDatabase database = new();

        await using JadlifyDbContext context = database.CreateContext();
        ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
        await lists.AddAsync(Build(Guid.NewGuid(), "First"));

        // The partial unique index is the database backstop to the handler's check.
        await Assert.ThrowsAsync<DbUpdateException>(() => lists.AddAsync(Build(Guid.NewGuid(), "Second")));
    }

    [Fact]
    public async Task CompletedList_FreesTheActiveSlotForANewList()
    {
        using SqliteTestDatabase database = new();
        var firstId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            await lists.AddAsync(Build(firstId, "First"));
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            ShoppingList first = (await lists.GetByIdAsync(firstId))!;
            first.Complete(Now.AddHours(1));
            await lists.UpdateAsync(first);

            // With the first list completed the partial unique index no longer covers it.
            await lists.AddAsync(Build(Guid.NewGuid(), "Second"));
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            ShoppingListRepository lists = new(verification, new TestCurrentUser(OwnerId));
            ShoppingList? active = await lists.GetActiveAsync();
            Assert.Equal("Second", active!.Name);
        }
    }

    [Fact]
    public async Task UpdateAsync_PersistsABoughtToggle()
    {
        using SqliteTestDatabase database = new();
        var listId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            await lists.AddAsync(Build(listId));
        }

        Guid itemId;
        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            ShoppingList list = (await lists.GetByIdAsync(listId))!;
            itemId = list.Items[0].Id;
            list.TrySetItemBought(itemId, true);
            await lists.UpdateAsync(list);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            ShoppingListRepository lists = new(verification, new TestCurrentUser(OwnerId));
            ShoppingList reloaded = (await lists.GetByIdAsync(listId))!;
            Assert.True(reloaded.Items.Single(i => i.Id == itemId).IsBought);
            Assert.Equal(1, reloaded.Version);
        }
    }

    [Fact]
    public async Task UpdateAsync_PersistsARefreshAtomically()
    {
        using SqliteTestDatabase database = new();
        var listId = Guid.NewGuid();
        var keptId = Guid.NewGuid();
        var removedId = Guid.NewGuid();
        var addedId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            var list = ShoppingList.Create(
                listId,
                "List",
                [Day1],
                [
                    Item(keptId, "Apples", 100m, null, Source(MealType.Breakfast, "Salad", 100m)),
                    Item(removedId, "Butter", 50m),
                ],
                "fp-1",
                Now);
            await lists.AddAsync(list);
        }

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            ShoppingList list = (await lists.GetByIdAsync(listId))!;
            list.ApplyRefresh(
                [
                    Item(keptId, "Apples", 150m, null, Source(MealType.Breakfast, "Salad", 150m)),
                    Item(addedId, "Milk", 500m),
                ],
                "fp-2");
            await lists.UpdateAsync(list);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            ShoppingListRepository lists = new(verification, new TestCurrentUser(OwnerId));
            ShoppingList reloaded = (await lists.GetByIdAsync(listId))!;

            Assert.Equal("fp-2", reloaded.SourceFingerprint);
            Assert.Equal(2, reloaded.Version);
            Assert.Equal(2, reloaded.Items.Count);
            Assert.Equal(150m, reloaded.Items.Single(i => i.ProductId == keptId).Grams);
            Assert.Contains(reloaded.Items, i => i.ProductId == addedId);
            Assert.DoesNotContain(reloaded.Items, i => i.ProductId == removedId);
        }
    }

    [Fact]
    public async Task ListAsync_OrdersActiveFirstThenCompletedNewestFirst()
    {
        using SqliteTestDatabase database = new();
        var olderCompletedId = Guid.NewGuid();
        var newerCompletedId = Guid.NewGuid();
        var activeId = Guid.NewGuid();

        await using (JadlifyDbContext context = database.CreateContext())
        {
            ShoppingListRepository lists = new(context, new TestCurrentUser(OwnerId));
            var older = ShoppingList.Create(
                olderCompletedId, "Older", [Day1], [Item(Guid.NewGuid(), "A", 1m)], "fp", Now);
            older.Complete(Now.AddHours(1));
            await lists.AddAsync(older);

            var newer = ShoppingList.Create(
                newerCompletedId, "Newer", [Day1], [Item(Guid.NewGuid(), "B", 1m)], "fp", Now.AddDays(1));
            newer.Complete(Now.AddDays(1).AddHours(1));
            await lists.AddAsync(newer);

            var active = ShoppingList.Create(
                activeId, "Active", [Day1], [Item(Guid.NewGuid(), "C", 1m)], "fp", Now.AddDays(2));
            await lists.AddAsync(active);
        }

        await using (JadlifyDbContext verification = database.CreateContext())
        {
            ShoppingListRepository lists = new(verification, new TestCurrentUser(OwnerId));
            IReadOnlyList<ShoppingList> all = await lists.ListAsync();

            Assert.Equal([activeId, newerCompletedId, olderCompletedId], all.Select(list => list.Id));
        }
    }

    private static ShoppingList Build(Guid id, string name = "List") =>
        ShoppingList.Create(id, name, [Day1], [Item(Guid.NewGuid(), "Oats", 100m)], "fp", Now);

    private static ShoppingProjectionItem Item(
        Guid productId,
        string name,
        decimal grams,
        ProductCategory? category = null,
        params ShoppingProjectionContribution[] contributions) =>
        new(
            productId,
            name,
            category,
            grams,
            contributions.Length == 0 ? [Source(MealType.Breakfast, name, grams)] : contributions);

    private static ShoppingProjectionContribution Source(MealType mealType, string label, decimal grams) =>
        new(Day1, mealType, label, grams);
}
