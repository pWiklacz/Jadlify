using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.GetProduct;

public sealed record GetProductQuery(Guid Id) : IQuery<ProductDto>;
