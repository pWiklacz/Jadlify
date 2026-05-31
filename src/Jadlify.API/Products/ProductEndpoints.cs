using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Products;
using Jadlify.Application.Products.CreateProduct;
using Jadlify.Application.Products.DeleteProduct;
using Jadlify.Application.Products.GetProduct;
using Jadlify.Application.Products.ListProducts;
using Jadlify.Application.Products.LookupBarcode;
using Jadlify.Application.Products.UpdateProduct;
using Jadlify.SharedKernel;

namespace Jadlify.API.Products;

/// <summary>
/// Maps the product CRUD + barcode-lookup use-cases onto <c>/api/products</c>. Every
/// route inherits the global fallback auth policy (authenticated + 'sub' claim), so none
/// opts out with AllowAnonymous. Each call goes through the mediator and shapes failures
/// with the shared <c>ResultExtensions.ToProblem</c> mapping.
/// </summary>
public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        RouteGroupBuilder products = app.MapGroup("/api/products");

        products.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ProductDto>> result =
                await mediator.QueryAsync(new ListProductsQuery(), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value.Select(ProductResponse.FromDto).ToArray())
                : result.ToProblem();
        });

        products.MapGet("/{id:guid}", async (Guid id, IMediator mediator, CancellationToken cancellationToken) =>
        {
            Result<ProductDto> result =
                await mediator.QueryAsync(new GetProductQuery(id), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ProductResponse.FromDto(result.Value))
                : result.ToProblem();
        });

        products.MapPost("/", async (
            CreateProductRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<Guid> result = await mediator.SendAsync(
                new CreateProductCommand(
                    request.Name,
                    request.Barcode,
                    request.Calories,
                    request.Protein,
                    request.Fat,
                    request.Carbohydrates),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            Guid id = result.Value;
            ProductResponse response = new(
                id,
                request.Name,
                NormalizeBarcode(request.Barcode),
                request.Calories,
                request.Protein,
                request.Fat,
                request.Carbohydrates);

            return Results.Created($"/api/products/{id}", response);
        });

        products.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(
                new UpdateProductCommand(
                    id,
                    request.Name,
                    request.Barcode,
                    request.Calories,
                    request.Protein,
                    request.Fat,
                    request.Carbohydrates),
                cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        products.MapDelete("/{id:guid}", async (
            Guid id,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result result = await mediator.SendAsync(new DeleteProductCommand(id), cancellationToken);

            return result.IsSuccess ? Results.NoContent() : result.ToProblem();
        });

        // Always HTTP 200: a miss (NotFound) is a normal outcome that routes the user into
        // manual entry, not an error (FR-006). The literal "barcode" segment never collides
        // with the GUID-constrained GET /{id:guid} route above.
        products.MapGet("/barcode/{barcode}", async (
            string barcode,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<BarcodeLookupResult> result =
                await mediator.QueryAsync(new LookupBarcodeQuery(barcode), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(BarcodeLookupResponse.FromResult(result.Value))
                : result.ToProblem();
        });

        return app;
    }

    // Mirror the handler's normalization so the 201 body matches what was persisted.
    private static string? NormalizeBarcode(string? barcode) =>
        string.IsNullOrWhiteSpace(barcode) ? null : barcode;
}
