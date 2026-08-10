using Jadlify.Application.Shopping.ShoppingLists;
using Jadlify.Application.Shopping.ShoppingLists.CompleteShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.CreateShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDetail;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListDiff;
using Jadlify.Application.Shopping.ShoppingLists.GetShoppingListIndex;
using Jadlify.Application.Shopping.ShoppingLists.RefreshShoppingList;
using Jadlify.Application.Shopping.ShoppingLists.ToggleShoppingListItem;
using Jadlify.Application.Tests.Planning;
using Jadlify.Application.Tests.Recipes;
using Jadlify.Domain.Nutrition;
using Jadlify.Domain.Planning;
using Jadlify.Domain.Recipes;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Shopping;

public class ShoppingListHandlerTests
{
    private static readonly DateOnly Day = new(2026, 6, 6);
    private static readonly CancellationToken None = CancellationToken.None;

    [Fact]
    public async Task Create_ProjectsItemsFromThePlan_AndPersistsAnActiveList()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        FakeShoppingListRepository lists = new();
        CreateShoppingListCommandHandler handler = new(
            lists, Plan(porridge, portions: 1), Recipes(porridge), TimeProvider.System);

        Result<ShoppingListDetailDto> result =
            await handler.HandleAsync(new CreateShoppingListCommand("Weekend", [Day]), None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDetailDto item = Assert.Single(result.Value.Items);
        Assert.Equal("Oats", item.ProductName);
        Assert.Equal(100m, item.Grams); // 200 g across 2 portions, one portion planned
        Assert.False(item.IsBought);
        Assert.Equal(1, result.Value.Version);
        Assert.Equal(1, lists.AddCount);
    }

    [Fact]
    public async Task Create_Fails_WhenAnActiveListAlreadyExists()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        FakeShoppingListRepository lists = new();
        CreateShoppingListCommandHandler handler = new(
            lists, Plan(porridge, 1), Recipes(porridge), TimeProvider.System);
        await handler.HandleAsync(new CreateShoppingListCommand("First", [Day]), None);

        Result<ShoppingListDetailDto> second =
            await handler.HandleAsync(new CreateShoppingListCommand("Second", [Day]), None);

        Assert.True(second.IsFailure);
        Assert.Equal("ShoppingList.ActiveExists", second.Error.Code);
    }

    [Fact]
    public async Task Toggle_TicksTheItem_AtTheCurrentVersion()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) =
            await CreateListAsync(porridge);
        ToggleShoppingListItemCommandHandler handler = new(lists);
        Guid itemId = detail.Items[0].Id;

        Result<ShoppingListDetailDto> result = await handler.HandleAsync(
            new ToggleShoppingListItemCommand(detail.Id, itemId, true, detail.Version), None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value.Items.Single(i => i.Id == itemId).IsBought);
    }

    [Fact]
    public async Task Toggle_Fails_OnVersionMismatch()
    {
        Recipe porridge = BuildPorridge(Guid.NewGuid());
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        ToggleShoppingListItemCommandHandler handler = new(lists);

        Result<ShoppingListDetailDto> result = await handler.HandleAsync(
            new ToggleShoppingListItemCommand(detail.Id, detail.Items[0].Id, true, detail.Version + 1), None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.VersionConflict", result.Error.Code);
    }

    [Fact]
    public async Task Toggle_Fails_WhenTheItemIsUnknown()
    {
        Recipe porridge = BuildPorridge(Guid.NewGuid());
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        ToggleShoppingListItemCommandHandler handler = new(lists);

        Result<ShoppingListDetailDto> result = await handler.HandleAsync(
            new ToggleShoppingListItemCommand(detail.Id, Guid.NewGuid(), true, detail.Version), None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.ItemNotFound", result.Error.Code);
    }

    [Fact]
    public async Task Diff_ReportsNoChanges_WhenThePlanIsUnchanged()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        GetShoppingListDiffQueryHandler handler = new(lists, Plan(porridge, 1), Recipes(porridge));

        Result<ShoppingListDiffDto> result =
            await handler.HandleAsync(new GetShoppingListDiffQuery(detail.Id), None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value.HasChanges);
    }

    [Fact]
    public async Task Diff_ReportsAChangedLine_AfterThePlanChanges()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        // The plan now uses two portions, doubling the oats.
        GetShoppingListDiffQueryHandler handler = new(lists, Plan(porridge, 2), Recipes(porridge));

        Result<ShoppingListDiffDto> result =
            await handler.HandleAsync(new GetShoppingListDiffQuery(detail.Id), None);

        Assert.True(result.Value.HasChanges);
        ShoppingListDiffLineDto changed = Assert.Single(result.Value.Changed);
        Assert.Equal(oatsId, changed.ProductId);
        Assert.Equal(100m, changed.PreviousGrams);
        Assert.Equal(200m, changed.NewGrams);
    }

    [Fact]
    public async Task Refresh_AppliesTheDiff_UnchecksChangedItem_AndBumpsVersion()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);

        // Shopper ticks the item, then the plan doubles the amount it needs.
        Guid itemId = detail.Items[0].Id;
        await new ToggleShoppingListItemCommandHandler(lists).HandleAsync(
            new ToggleShoppingListItemCommand(detail.Id, itemId, true, detail.Version), None);

        FakeMealPlanRepository changedPlan = Plan(porridge, 2);
        FakeRecipeRepository recipes = Recipes(porridge);
        ShoppingListDiffDto diff = (await new GetShoppingListDiffQueryHandler(lists, changedPlan, recipes)
            .HandleAsync(new GetShoppingListDiffQuery(detail.Id), None)).Value;

        Result<ShoppingListDetailDto> result = await new RefreshShoppingListCommandHandler(lists, changedPlan, recipes)
            .HandleAsync(new RefreshShoppingListCommand(detail.Id, detail.Version, diff.SourceFingerprint), None);

        Assert.True(result.IsSuccess);
        ShoppingListItemDetailDto item = result.Value.Items.Single(i => i.ProductId == oatsId);
        Assert.Equal(200m, item.Grams);
        Assert.False(item.IsBought); // quantity changed -> tick cleared
        Assert.Equal(detail.Version + 1, result.Value.Version);
    }

    [Fact]
    public async Task Refresh_Fails_WhenThePlanChangedAgainSinceThePreview()
    {
        var oatsId = Guid.NewGuid();
        Recipe porridge = BuildPorridge(oatsId);
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);

        // Preview against a two-portion plan, then apply against a three-portion plan.
        ShoppingListDiffDto preview = (await new GetShoppingListDiffQueryHandler(lists, Plan(porridge, 2), Recipes(porridge))
            .HandleAsync(new GetShoppingListDiffQuery(detail.Id), None)).Value;

        Result<ShoppingListDetailDto> result = await new RefreshShoppingListCommandHandler(lists, Plan(porridge, 3), Recipes(porridge))
            .HandleAsync(new RefreshShoppingListCommand(detail.Id, detail.Version, preview.SourceFingerprint), None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.SourceChanged", result.Error.Code);
    }

    [Fact]
    public async Task Complete_FreezesTheList_AndBumpsVersion()
    {
        Recipe porridge = BuildPorridge(Guid.NewGuid());
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        CompleteShoppingListCommandHandler handler = new(lists, TimeProvider.System);

        Result<ShoppingListDetailDto> result =
            await handler.HandleAsync(new CompleteShoppingListCommand(detail.Id, detail.Version), None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value.Status);
        Assert.NotNull(result.Value.CompletedAt);
        Assert.Equal(detail.Version + 1, result.Value.Version);
    }

    [Fact]
    public async Task Complete_Fails_OnVersionMismatch()
    {
        Recipe porridge = BuildPorridge(Guid.NewGuid());
        (FakeShoppingListRepository lists, ShoppingListDetailDto detail) = await CreateListAsync(porridge);
        CompleteShoppingListCommandHandler handler = new(lists, TimeProvider.System);

        Result<ShoppingListDetailDto> result =
            await handler.HandleAsync(new CompleteShoppingListCommand(detail.Id, detail.Version + 5), None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.VersionConflict", result.Error.Code);
    }

    [Fact]
    public async Task Detail_Fails_ForAnUnknownList()
    {
        FakeShoppingListRepository lists = new();
        GetShoppingListDetailQueryHandler handler = new(lists);

        Result<ShoppingListDetailDto> result =
            await handler.HandleAsync(new GetShoppingListDetailQuery(Guid.NewGuid()), None);

        Assert.True(result.IsFailure);
        Assert.Equal("ShoppingList.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Index_SplitsTheActiveListFromCompletedHistory()
    {
        Recipe porridge = BuildPorridge(Guid.NewGuid());
        FakeShoppingListRepository lists = new();
        CreateShoppingListCommandHandler create = new(lists, Plan(porridge, 1), Recipes(porridge), TimeProvider.System);

        ShoppingListDetailDto first =
            (await create.HandleAsync(new CreateShoppingListCommand("Old", [Day]), None)).Value;
        await new CompleteShoppingListCommandHandler(lists, TimeProvider.System)
            .HandleAsync(new CompleteShoppingListCommand(first.Id, first.Version), None);
        ShoppingListDetailDto second =
            (await create.HandleAsync(new CreateShoppingListCommand("Current", [Day]), None)).Value;

        Result<ShoppingListIndexDto> result =
            await new GetShoppingListIndexQueryHandler(lists).HandleAsync(new GetShoppingListIndexQuery(), None);

        Assert.NotNull(result.Value.Active);
        Assert.Equal(second.Id, result.Value.Active!.Id);
        Assert.Equal(first.Id, Assert.Single(result.Value.History).Id);
    }

    private static async Task<(FakeShoppingListRepository Lists, ShoppingListDetailDto Detail)> CreateListAsync(
        Recipe porridge)
    {
        FakeShoppingListRepository lists = new();
        CreateShoppingListCommandHandler create = new(
            lists, Plan(porridge, 1), Recipes(porridge), TimeProvider.System);
        Result<ShoppingListDetailDto> result =
            await create.HandleAsync(new CreateShoppingListCommand("List", [Day]), None);
        return (lists, result.Value);
    }

    private static Recipe BuildPorridge(Guid oatsId)
    {
        var recipe = new Recipe(Guid.NewGuid(), "Porridge", 2);
        recipe.AddIngredient(new RecipeIngredient(
            oatsId, "Oats", new MacroNutrients(380m, 13m, 7m, 60m), new GramAmount(200m)));
        return recipe;
    }

    private static FakeMealPlanRepository Plan(Recipe recipe, decimal portions) =>
        new(MealPlanEntry.ForRecipe(Guid.NewGuid(), Day, recipe.Id, MealType.Breakfast, portions));

    private static FakeRecipeRepository Recipes(Recipe recipe) => new(recipe);
}
