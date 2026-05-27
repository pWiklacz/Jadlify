using Jadlify.SharedKernel;
using System;
using System.Collections.Generic;
using System.Text;

namespace Jadlify.Application.Common.Mediator
{
    public interface IMediator
    {
        Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default);
        Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default);
        Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default);
    }
}
