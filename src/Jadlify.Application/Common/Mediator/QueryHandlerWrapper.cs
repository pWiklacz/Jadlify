using Jadlify.Application.Common.Behaviours;
using Jadlify.SharedKernel;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jadlify.Application.Common.Mediator
{
    internal abstract class QueryHandlerWrapperBase<TResponse>
    {
        public abstract Task<Result<TResponse>> HandleAsync(IQuery<TResponse> query, IServiceProvider serviceProvider, CancellationToken cancellationToken);
    }

    // Wrapper dla IQuery<TResponse>
    internal class QueryHandlerWrapperImpl<TQuery, TResponse> : QueryHandlerWrapperBase<TResponse>
        where TQuery : IQuery<TResponse>
    {
        public override async Task<Result<TResponse>> HandleAsync(IQuery<TResponse> query, IServiceProvider serviceProvider, CancellationToken cancellationToken)
        {
            var handler = serviceProvider.GetService<IQueryHandler<TQuery, TResponse>>()
                ?? throw new InvalidOperationException($"Handler for query {typeof(TQuery).Name} not registered in DI container.");

            var behaviors = serviceProvider.GetServices<IPipelineBehavior<TQuery, TResponse>>().ToList();

            Task<Result<TResponse>> HandlerDelegate() => handler.HandleAsync((TQuery)query, cancellationToken);

            return await PipelineHelper.Execute(behaviors, (TQuery)query, HandlerDelegate, cancellationToken);
        }
    }
}
