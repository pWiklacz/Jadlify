using Jadlify.Application.Identity;
using Jadlify.Application.Products;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;
using Microsoft.EntityFrameworkCore;

namespace Jadlify.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository : IProductRepository
{
    private static readonly Error NotFound =
        Error.NotFound("Product.NotFound", "The product was not found for the current user.");

    private static readonly Error InUse =
        Error.Conflict("Product.InUse", "The product is used by a recipe and cannot be deleted.");

    private readonly JadlifyDbContext _context;
    private readonly ICurrentUser _currentUser;

    public ProductRepository(JadlifyDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        _context.Products.Add(product);
        _context.Entry(product).Property(PersistenceConstants.UserIdProperty).CurrentValue =
            _currentUser.UserId.Value;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.Products.SingleOrDefaultAsync(
            product => product.Id == id
                && EF.Property<string>(product, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        return await _context.Products
            .Where(product => EF.Property<string>(product, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListByIdsAsync(
        IReadOnlyCollection<Guid> ids,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(ids);

        if (ids.Count == 0)
        {
            return [];
        }

        string owner = _currentUser.UserId.Value;

        return await _context.Products
            .Where(product => ids.Contains(product.Id)
                && EF.Property<string>(product, PersistenceConstants.UserIdProperty) == owner)
            .ToListAsync(cancellationToken);
    }

    public async Task<Product?> GetByBarcodeAsync(string barcode, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(barcode);

        string owner = _currentUser.UserId.Value;

        return await _context.Products.FirstOrDefaultAsync(
            product => product.Barcode == barcode
                && EF.Property<string>(product, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
    }

    public async Task<Result> UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        string owner = _currentUser.UserId.Value;

        Product? existing = await _context.Products.SingleOrDefaultAsync(
            candidate => candidate.Id == product.Id
                && EF.Property<string>(candidate, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(NotFound);
        }

        _context.Entry(existing).CurrentValues.SetValues(product);
        _context.Entry(existing).Reference(p => p.Per100Grams).TargetEntry!
            .CurrentValues.SetValues(product.Per100Grams);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        string owner = _currentUser.UserId.Value;

        Product? existing = await _context.Products.SingleOrDefaultAsync(
            product => product.Id == id
                && EF.Property<string>(product, PersistenceConstants.UserIdProperty) == owner,
            cancellationToken);
        if (existing is null)
        {
            return Result.Fail(NotFound);
        }

        bool inUse = await _context.Recipes
            .Where(recipe => EF.Property<string>(recipe, PersistenceConstants.UserIdProperty) == owner)
            .AnyAsync(
                recipe => recipe.Ingredients.Any(ingredient => ingredient.ProductId == id),
                cancellationToken);
        if (inUse)
        {
            return Result.Fail(InUse);
        }

        _context.Products.Remove(existing);
        await _context.SaveChangesAsync(cancellationToken);
        return Result.Ok();
    }
}
