using Jadlify.Domain.Planning;
using Jadlify.Domain.Products;

namespace Jadlify.Application.Planning;

/// <summary>
/// Read model for one planned meal. The variant fields are mutually exclusive and mirror the
/// domain's exactly-one-source rule: a recipe entry fills <see cref="RecipeId"/>,
/// <see cref="RecipeName"/>, and <see cref="Portions"/>; a product entry fills
/// <see cref="ProductId"/>, <see cref="ProductName"/>, <see cref="Category"/>, and
/// <see cref="Grams"/>. Keeping the units in separately named fields means a consumer never
/// has to infer whether a bare number means portions or grams.
/// </summary>
/// <remarks>
/// Recipe entries reference the live recipe, so the name is resolved from the owner-scoped
/// recipe repository at read time. Product entries carry their own snapshot, so their name
/// and category come straight from the entry and survive a catalog edit or deletion.
/// </remarks>
public sealed record MealPlanEntryDto(
    Guid Id,
    DateOnly Date,
    MealType MealType,
    MealPlanEntrySource Source,
    Guid? RecipeId,
    string? RecipeName,
    decimal? Portions,
    Guid? ProductId,
    string? ProductName,
    ProductCategory? Category,
    decimal? Grams)
{
    /// <summary>
    /// Projects a domain entry. <paramref name="recipeName"/> is used only for a recipe entry
    /// and falls back to an empty string when the recipe could not be resolved.
    /// </summary>
    public static MealPlanEntryDto FromDomain(MealPlanEntry entry, string? recipeName = null)
    {
        ArgumentNullException.ThrowIfNull(entry);

        return entry.Source is MealPlanEntrySource.Product
            ? new MealPlanEntryDto(
                entry.Id,
                entry.Date,
                entry.MealType,
                entry.Source,
                RecipeId: null,
                RecipeName: null,
                Portions: null,
                entry.Product!.ProductId,
                entry.Product.Name,
                entry.Product.Category,
                entry.Quantity)
            : new MealPlanEntryDto(
                entry.Id,
                entry.Date,
                entry.MealType,
                entry.Source,
                entry.RecipeId,
                recipeName ?? string.Empty,
                entry.Quantity,
                ProductId: null,
                ProductName: null,
                Category: null,
                Grams: null);
    }
}
