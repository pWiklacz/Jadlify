using Jadlify.SharedKernel;

namespace Jadlify.Application.Common.Behaviours;

public interface IPipelineBehavior<TRequest, TResponse>
{
    Task<Result<TResponse>> HandleAsync(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken = default);
}

public delegate Task<Result<TResponse>> RequestHandlerDelegate<TResponse>();
