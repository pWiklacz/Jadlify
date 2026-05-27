using Jadlify.Application.Common.Behaviours;
using Jadlify.SharedKernel;

namespace Jadlify.Application.Common.Mediator
{
    internal static class PipelineHelper
    {
        public static async Task<Result<TResponse>> Execute<TRequest, TResponse>(
            IReadOnlyList<IPipelineBehavior<TRequest, TResponse>> behaviors,
            TRequest request,
            Func<Task<Result<TResponse>>> handlerFunc,
            CancellationToken cancellationToken)
        {
            if (behaviors.Count == 0)
            {
                return await handlerFunc();
            }

            RequestHandlerDelegate<TResponse> currentHandler = () => handlerFunc();

            for (int i = behaviors.Count - 1; i >= 0; i--)
            {
                var behavior = behaviors[i];
                var nextHandler = currentHandler;

                currentHandler = () => behavior.HandleAsync(request, nextHandler, cancellationToken);
            }

            return await currentHandler();
        }
    }
}
