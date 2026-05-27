using Jadlify.SharedKernel;

namespace Jadlify.Application.Common.Mediator;

public interface IQueryHandler<in TQuery, TResponse>
   where TQuery : IQuery<TResponse>
{
    Task<Result<TResponse>> HandleAsync(TQuery query, CancellationToken cancellationToken);
}
