using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.LookupBarcode;

public sealed record LookupBarcodeQuery(string Barcode) : IQuery<BarcodeLookupResult>;
