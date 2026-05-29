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

    Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default);

    Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default);

    Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes the product when it is not referenced by any recipe ingredient.
    /// Returns a failure result instead of cascading dependent user data.
    /// </summary>
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
