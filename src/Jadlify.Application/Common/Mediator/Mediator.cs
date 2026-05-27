using Jadlify.SharedKernel;
using System.Collections.Concurrent;

namespace Jadlify.Application.Common.Mediator
{
    public class Mediator : IMediator
    {
        private readonly IServiceProvider _serviceProvider;

        // Osobne cache dla każdego typu wrappera — eliminuje ryzyko błędnego castu
        private static readonly ConcurrentDictionary<Type, CommandHandlerWrapperBase> _commandWrappers = new();
        private static readonly ConcurrentDictionary<Type, object> _commandWithResponseWrappers = new();
        private static readonly ConcurrentDictionary<Type, object> _queryWrappers = new();

        public Mediator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task<Result> SendAsync(ICommand command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var wrapper = _commandWrappers.GetOrAdd(
                command.GetType(),
                static t => CreateWrapper<CommandHandlerWrapperBase>(typeof(CommandHandlerWrapperImpl<>), t)
            );

            return wrapper.HandleAsync(command, _serviceProvider, cancellationToken);
        }

        public Task<Result<TResponse>> SendAsync<TResponse>(ICommand<TResponse> command, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(command);

            var wrapper = (CommandHandlerWithResponseWrapperBase<TResponse>)_commandWithResponseWrappers.GetOrAdd(
                command.GetType(),
                static t => CreateWrapper<object>(typeof(CommandHandlerWithResponseWrapperImpl<,>), t, typeof(TResponse))
            );

            return wrapper.HandleAsync(command, _serviceProvider, cancellationToken);
        }

        public Task<Result<TResponse>> QueryAsync<TResponse>(IQuery<TResponse> query, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(query);

            var wrapper = (QueryHandlerWrapperBase<TResponse>)_queryWrappers.GetOrAdd(
                query.GetType(),
                static t => CreateWrapper<object>(typeof(QueryHandlerWrapperImpl<,>), t, typeof(TResponse))
            );

            return wrapper.HandleAsync(query, _serviceProvider, cancellationToken);
        }

        private static TWrapper CreateWrapper<TWrapper>(Type genericWrapperType, params Type[] typeArgs)
        {
            var closedType = genericWrapperType.MakeGenericType(typeArgs);
            var instance = Activator.CreateInstance(closedType);

            if (instance is not TWrapper wrapper)
            {
                throw new InvalidOperationException(
                    $"Failed to create wrapper of type {closedType.Name}. " +
                    $"Ensure it has a parameterless constructor.");
            }

            return wrapper;
        }
    }
}
