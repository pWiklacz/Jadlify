using Jadlify.Application.Products;

namespace Jadlify.API.Tests.Common;

/// <summary>
/// In-memory <see cref="IBarcodeProductLookup"/> stub the tests control via <see cref="OnLookup"/>.
/// Defaults to "no data" (returns <c>null</c>), the same not-found contract the real OFF adapter
/// honours so a miss never blocks the flow.
/// </summary>
internal sealed class StubBarcodeProductLookup : IBarcodeProductLookup
{
    public Func<string, BarcodeProductData?> OnLookup { get; set; } = _ => null;

    public Task<BarcodeProductData?> LookupAsync(string barcode, CancellationToken cancellationToken = default) =>
        Task.FromResult(OnLookup(barcode));
}
