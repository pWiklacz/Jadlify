using Jadlify.SharedKernel;

namespace Jadlify.API.Common;

/// <summary>
/// Single place that turns a failed <see cref="Result"/> / <see cref="Result{TValue}"/>
/// into an <see cref="IResult"/>, mapping <see cref="ErrorType"/> to a status code so
/// every current and future endpoint shapes failures identically. Success status codes
/// (200/201/204) are picked by the calling endpoint; this helper only handles failures.
/// It never echoes tokens or internal exception detail (NFR "Prywatność operacyjna").
/// </summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (result.IsSuccess)
        {
            throw new InvalidOperationException(
                "A successful result has no problem representation; the endpoint shapes success.");
        }

        // A validation failure carries one Error per field; surface them as field messages.
        if (result.Error is ValidationError validationError)
        {
            return Results.ValidationProblem(
                ToFieldErrors(validationError),
                statusCode: StatusCodes.Status400BadRequest);
        }

        return result.Error.Type switch
        {
            ErrorType.NotFound => Problem(StatusCodes.Status404NotFound, "Not Found", result.Error.Description),
            ErrorType.Conflict => Problem(StatusCodes.Status409Conflict, "Conflict", result.Error.Description),
            ErrorType.Forbidden => Problem(StatusCodes.Status403Forbidden, "Forbidden", result.Error.Description),
            // A Problem is an unexpected server-side failure: surface a generic 500 and
            // deliberately withhold the internal description.
            ErrorType.Problem => Problem(StatusCodes.Status500InternalServerError, "Server Error", detail: null),
            // Validation handled above; everything else (Failure) is a 400 with a safe message.
            _ => Problem(StatusCodes.Status400BadRequest, "Bad Request", result.Error.Description),
        };
    }

    private static IResult Problem(int statusCode, string title, string? detail) =>
        Results.Problem(statusCode: statusCode, title: title, detail: detail);

    private static IDictionary<string, string[]> ToFieldErrors(ValidationError validationError) =>
        validationError.Errors
            .GroupBy(error => error.Code, error => error.Description)
            .ToDictionary(group => group.Key, group => group.ToArray());
}
