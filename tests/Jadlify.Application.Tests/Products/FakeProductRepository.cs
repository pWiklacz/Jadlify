using Jadlify.Application.Products;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Tests.Products;

/// <summary>
/// In-memory <see cref="IProductRepository"/> for handler unit tests. Mirrors the
/// real repository's Result contract (Update/Delete return NotFound when the
/// product is absent) but without persistence or owner scoping — the handlers
/// under test contain no scoping logic of their own.
/// </summary>
internal sealed class FakeProductRepository : IProductRepository
{
    private static readonly Error NotFound =
        Error.NotFound("Product.NotFound", "The product was not found for the current user.");

    private readonly List<Product> _products;

    public FakeProductRepository(params Product[] seed)
    {
        _products = [.. seed];
    }

    public IReadOnlyList<Product> Products => _products;

    public Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        _products.Add(product);
        return Task.CompletedTask;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.SingleOrDefault(p => p.Id == id));

    public Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>([.. _products]);

    public Task<IReadOnlyList<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Product>>([.. _products.Where(p => ids.Contains(p.Id))]);

    public Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default) =>
        Task.FromResult(_products.FirstOrDefault(p => p.Barcode == barcode));

    public Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        int index = _products.FindIndex(p => p.Id == product.Id);
        if (index < 0)
        {
            return Task.FromResult(Result.Fail(NotFound));
        }

        _products[index] = product;
        return Task.FromResult(Result.Ok());
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        int removed = _products.RemoveAll(p => p.Id == id);
        return Task.FromResult(removed > 0 ? Result.Ok() : Result.Fail(NotFound));
    }
}
