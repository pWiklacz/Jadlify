using Jadlify.API.Common;
using Jadlify.Application.Common.Mediator;
using Jadlify.Application.Products;
using Jadlify.Application.Products.CreateProduct;
using Jadlify.Application.Products.DeleteProduct;
using Jadlify.Application.Products.GetProduct;
using Jadlify.Application.Products.GetProductCatalog;
using Jadlify.Application.Products.ListProducts;
using Jadlify.Application.Products.LookupBarcode;
using Jadlify.Application.Products.UpdateProduct;
using Jadlify.Domain.Products;
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

        products.MapGet("/", async (
            string? search,
            int? skip,
            int? take,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<IReadOnlyList<ProductDto>> result =
                await mediator.QueryAsync(new ListProductsQuery(search, skip, take), cancellationToken);

            return result.IsSuccess
                ? Results.Ok(result.Value.Select(ProductResponse.FromDto).ToArray())
                : result.ToProblem();
        });

        // Paginated catalog: search + category filter + sort, returning items/total/skip/take.
        // The literal "catalog" segment never collides with the GUID-constrained GET /{id:guid}.
        products.MapGet("/catalog", async (
            string? search,
            string? category,
            string? sort,
            int? skip,
            int? take,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<CatalogCategoryFilter> filter = ParseCatalogCategory(category);
            if (filter.IsFailure)
            {
                return filter.ToProblem();
            }

            Result<ProductCatalogSort> sortOrder = ParseCatalogSort(sort);
            if (sortOrder.IsFailure)
            {
                return sortOrder.ToProblem();
            }

            Result<ProductCatalogPageDto> result = await mediator.QueryAsync(
                new GetProductCatalogQuery(
                    search,
                    filter.Value.Category,
                    filter.Value.UncategorizedOnly,
                    sortOrder.Value,
                    skip,
                    take),
                cancellationToken);

            return result.IsSuccess
                ? Results.Ok(ProductCatalogResponse.FromDto(result.Value))
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
            Result<ProductCategory?> category = ParseOptionalCategory(request.Category);
            if (category.IsFailure)
            {
                return category.ToProblem();
            }

            Result<Guid> result = await mediator.SendAsync(
                new CreateProductCommand(
                    request.Name,
                    request.Barcode,
                    request.Calories,
                    request.Protein,
                    request.Fat,
                    request.Carbohydrates,
                    request.PackageSizeGrams,
                    request.SaturatedFat,
                    request.MonounsaturatedFat,
                    request.PolyunsaturatedFat,
                    request.TransFat,
                    request.Sugars,
                    request.Fiber,
                    request.Salt,
                    request.Sodium,
                    request.Potassium,
                    request.Calcium,
                    request.Iron,
                    request.VitaminA,
                    request.VitaminC,
                    request.VitaminD,
                    request.Brand,
                    category.Value),
                cancellationToken);

            if (result.IsFailure)
            {
                return result.ToProblem();
            }

            Guid id = result.Value;
            var response = ProductResponse.Created(id, request, category.Value);

            return Results.Created($"/api/products/{id}", response);
        });

        products.MapPut("/{id:guid}", async (
            Guid id,
            UpdateProductRequest request,
            IMediator mediator,
            CancellationToken cancellationToken) =>
        {
            Result<ProductCategory?> category = ParseOptionalCategory(request.Category);
            if (category.IsFailure)
            {
                return category.ToProblem();
            }

            Result result = await mediator.SendAsync(
                new UpdateProductCommand(
                    id,
                    request.Name,
                    request.Barcode,
                    request.Calories,
                    request.Protein,
                    request.Fat,
                    request.Carbohydrates,
                    request.PackageSizeGrams,
                    request.SaturatedFat,
                    request.MonounsaturatedFat,
                    request.PolyunsaturatedFat,
                    request.TransFat,
                    request.Sugars,
                    request.Fiber,
                    request.Salt,
                    request.Sodium,
                    request.Potassium,
                    request.Calcium,
                    request.Iron,
                    request.VitaminA,
                    request.VitaminC,
                    request.VitaminD,
                    request.Brand,
                    category.Value),
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

    // null/empty -> null ("Bez kategorii", a valid state). A stable enum name -> that category.
    // A numeric or unknown value is a 400 field error rather than a silently defaulted category.
    private static Result<ProductCategory?> ParseOptionalCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Ok<ProductCategory?>(null);
        }

        if (Enum.TryParse(value, ignoreCase: false, out ProductCategory category) && Enum.IsDefined(category))
        {
            return Result.Ok<ProductCategory?>(category);
        }

        return Result.Fail<ProductCategory?>(CategoryValidationError());
    }

    // Catalog category filter: null/empty -> all; the "None" sentinel -> uncategorized only;
    // a stable enum name -> that category; anything else -> 400.
    private static Result<CatalogCategoryFilter> ParseCatalogCategory(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Ok(new CatalogCategoryFilter(null, UncategorizedOnly: false));
        }

        if (string.Equals(value, "None", StringComparison.Ordinal))
        {
            return Result.Ok(new CatalogCategoryFilter(null, UncategorizedOnly: true));
        }

        if (Enum.TryParse(value, ignoreCase: false, out ProductCategory category) && Enum.IsDefined(category))
        {
            return Result.Ok(new CatalogCategoryFilter(category, UncategorizedOnly: false));
        }

        return Result.Fail<CatalogCategoryFilter>(CategoryValidationError());
    }

    // null/empty -> the default NameAsc; a stable enum name -> that sort; anything else -> 400.
    private static Result<ProductCatalogSort> ParseCatalogSort(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Ok(ProductCatalogSort.NameAsc);
        }

        if (Enum.TryParse(value, ignoreCase: false, out ProductCatalogSort sort) && Enum.IsDefined(sort))
        {
            return Result.Ok(sort);
        }

        return Result.Fail<ProductCatalogSort>(new ValidationError(
        [
            new Error(
                "Sort",
                "The sort must be one of NameAsc, CaloriesAsc, or Category.",
                ErrorType.Validation)
        ]));
    }

    private static ValidationError CategoryValidationError() =>
        new(
        [
            new Error(
                "Category",
                "The category must be one of the supported product categories.",
                ErrorType.Validation)
        ]);

    private readonly record struct CatalogCategoryFilter(ProductCategory? Category, bool UncategorizedOnly);
}
