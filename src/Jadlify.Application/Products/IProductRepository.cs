using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products;

/// <summary>
/// Owner-scoped persistence for user products. Every operation is implicitly
/// scoped to the authenticated user; implementations must never expose another
/// user's products.
/// </summary>
public interface IProductRepository
{
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListAsync(
        string? search = null,
        int skip = 0,
        int take = ProductListBounds.DefaultTake,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the owned product. Recipe ingredients keep historical product snapshots,
    /// so product deletion is not blocked by recipe references.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
