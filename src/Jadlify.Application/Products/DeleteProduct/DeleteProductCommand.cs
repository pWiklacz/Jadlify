using Jadlify.Application.Common.Mediator;

namespace Jadlify.Application.Products.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand;
