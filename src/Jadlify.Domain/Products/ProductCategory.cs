namespace Jadlify.Domain.Products;

/// <summary>
/// The canonical product taxonomy. Members use stable English names that never change
/// once shipped — the wire and persistence both key off these names, and the UI maps each
/// to a Polish label. Declaration order is the curated shopping order (produce first,
/// "Inne"/Other last). A <c>null</c> <see cref="Product.Category"/> means "Bez kategorii"
/// (uncategorized), which is always a valid state and never blocks a save.
/// </summary>
public enum ProductCategory
{
    Vegetables,
    Fruits,
    MeatAndFish,
    Dairy,
    GrainsAndBread,
    PantryAndDryGoods,
    Frozen,
    Beverages,
    Other,
}
