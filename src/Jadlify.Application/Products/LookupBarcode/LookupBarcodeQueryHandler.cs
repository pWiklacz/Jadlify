using Jadlify.Application.Common.Mediator;
using Jadlify.Domain.Products;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Products.LookupBarcode;

public sealed class LookupBarcodeQueryHandler : IQueryHandler<LookupBarcodeQuery, BarcodeLookupResult>
{
    private readonly IProductRepository _products;
    private readonly IBarcodeProductLookup _lookup;

    public LookupBarcodeQueryHandler(IProductRepository products, IBarcodeProductLookup lookup)
    {
        _products = products;
        _lookup = lookup;
    }

    public async Task<Result<BarcodeLookupResult>> HandleAsync(
        LookupBarcodeQuery query,
        CancellationToken cancellationToken)
    {
        // Dedupe first: an own-catalog hit short-circuits before we ever call OFF,
        // so a barcode the user already saved is offered for edit, not re-fetched.
        Product? existing = await _products.GetByBarcodeAsync(query.Barcode, cancellationToken);
        if (existing is not null)
        {
            return Result.Ok(
                BarcodeLookupResult.AlreadyInCatalog(query.Barcode, ProductDto.FromDomain(existing)));
        }

        // Fall through to the external source. A miss or an outage returns null,
        // which is a normal NotFound outcome — never an error (FR-006).
        BarcodeProductData? data = await _lookup.LookupAsync(query.Barcode, cancellationToken);

        return Result.Ok(
            data is null
                ? BarcodeLookupResult.NotFound(query.Barcode)
                : BarcodeLookupResult.Found(query.Barcode, data));
    }
}
