using Jadlify.Application.Products;

namespace Jadlify.Application.Tests.Products;

/// <summary>
/// Controllable <see cref="IBarcodeProductLookup"/> stub. Returns a fixed result
/// and counts calls so tests can assert the dedupe rule (catalog hit must not
/// reach the external source).
/// </summary>
internal sealed class FakeBarcodeProductLookup : IBarcodeProductLookup
{
    private readonly BarcodeProductData? _result;

    public FakeBarcodeProductLookup(BarcodeProductData? result = null)
    {
        _result = result;
    }

    public int CallCount { get; private set; }

    public Task<BarcodeProductData?> LookupAsync(string barcode, CancellationToken cancellationToken = default)
    {
        CallCount++;
        return Task.FromResult(_result);
    }
}
