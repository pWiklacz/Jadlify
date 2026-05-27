using FluentValidation;
using Jadlify.SharedKernel;
using Microsoft.Extensions.Logging;

namespace Jadlify.Application.Common.Behaviours
{
    public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    {
        private readonly IEnumerable<IValidator<TRequest>> _validators;
        private readonly ILogger<ValidationBehavior<TRequest, TResponse>> _logger;

        public ValidationBehavior(
            IEnumerable<IValidator<TRequest>> validators,
            ILogger<ValidationBehavior<TRequest, TResponse>> logger)
        {
            _validators = validators;
            _logger = logger;
        }

        public async Task<Result<TResponse>> HandleAsync(
            TRequest request,
            RequestHandlerDelegate<TResponse> next,
            CancellationToken cancellationToken = default)
        {
            // Jeśli nie ma validatorów, przejdź dalej
            if (!_validators.Any())
            {
                return await next();
            }

            // Kontekst walidacji
            var context = new ValidationContext<TRequest>(request);

            // Wykonaj wszystkie walidacje równolegle
            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken))
            );

            // Zbierz wszystkie błędy
            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToArray();

            // Jeśli są błędy walidacji
            if (failures.Length != 0)
            {
                // Grupuj błędy według właściwości
                var errorMessages = failures
                    .GroupBy(x => x.PropertyName)
                    .Select(g => $"{g.Key}: {string.Join(", ", g.Select(x => x.ErrorMessage))}")
                    .ToList();

                var errors = string.Join("; ", errorMessages);

                _logger.LogWarning(
                    "Validation failed for {RequestName}: {Errors}",
                    typeof(TRequest).Name,
                    errors
                );

                var validationErrors = failures
                    .Select(f => new Error(f.PropertyName, f.ErrorMessage, ErrorType.Validation))
                    .ToArray();

                return Result.Fail<TResponse>(new ValidationError(validationErrors));
            }

            // Kontynuuj pipeline jeśli walidacja przeszła
            return await next();
        }
    }
}
