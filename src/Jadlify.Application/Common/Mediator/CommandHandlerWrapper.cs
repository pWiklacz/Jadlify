using Jadlify.Application.Common.Behaviours;
using Jadlify.SharedKernel;
using Microsoft.Extensions.DependencyInjection;

namespace Jadlify.Application.Common.Mediator;

internal abstract class CommandHandlerWrapperBase
{
    public abstract Task<Result> HandleAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

// Wrapper dla ICommand (bez TResponse)
internal class CommandHandlerWrapperImpl<TCommand> : CommandHandlerWrapperBase
    where TCommand : ICommand
{
    public override async Task<Result> HandleAsync(ICommand command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ICommandHandler<TCommand> handler = serviceProvider.GetService<ICommandHandler<TCommand>>()
          ?? throw new InvalidOperationException($"Handler for command {typeof(TCommand).Name} not registered in DI container.");

        // Tłumaczymy Result na Result<object> aby dopasować się do IPipelineBehavior<T, TResponse>
        async Task<Result<object>> HandlerDelegate()
        {
            Result result = await handler.HandleAsync((TCommand)command, cancellationToken);
            return result.IsSuccess ? Result.Ok<object>(null!) : Result.Fail<object>(result.Error);
        }

        // Rejestrujesz zachowania dla zwykłych komend jako IPipelineBehavior<TCommand, object>
        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, object>>().ToList();

        Result<object> pipelineResult = await PipelineHelper.Execute(behaviors, (TCommand)command, HandlerDelegate, cancellationToken);

        return pipelineResult.ToResult();
    }
}

internal abstract class CommandHandlerWithResponseWrapperBase<TResponse>
{
    public abstract Task<Result<TResponse>> HandleAsync(ICommand<TResponse> command, IServiceProvider serviceProvider, CancellationToken cancellationToken);
}

// Wrapper dla ICommand<TResponse>
internal class CommandHandlerWithResponseWrapperImpl<TCommand, TResponse> : CommandHandlerWithResponseWrapperBase<TResponse>
    where TCommand : ICommand<TResponse>
{
    public override async Task<Result<TResponse>> HandleAsync(ICommand<TResponse> command, IServiceProvider serviceProvider, CancellationToken cancellationToken)
    {
        ICommandHandler<TCommand, TResponse> handler = serviceProvider.GetService<ICommandHandler<TCommand, TResponse>>()
            ?? throw new InvalidOperationException($"Handler for command {typeof(TCommand).Name} not registered in DI container.");

        var behaviors = serviceProvider.GetServices<IPipelineBehavior<TCommand, TResponse>>().ToList();

        Task<Result<TResponse>> HandlerDelegate() => handler.HandleAsync((TCommand)command, cancellationToken);

        return await PipelineHelper.Execute(behaviors, (TCommand)command, HandlerDelegate, cancellationToken);
    }
}
