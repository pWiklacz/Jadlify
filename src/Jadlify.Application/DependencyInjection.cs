using FluentValidation;
using Jadlify.Application.Common.Behaviours;
using Jadlify.Application.Common.Mediator;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Jadlify.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        // 1. Rejestracja CQRS (Mediator, Handlers, Validators, Pipelines)
        services.AddCQRS(assembly);

        return services;
    }

    public static IServiceCollection AddCQRS(this IServiceCollection services, params Assembly[] assemblies)
    {
        var assembliesToScan = assemblies?.Length > 0
            ? assemblies
            : new[] { Assembly.GetExecutingAssembly() };

        services.AddScoped<IMediator, Mediator>();
        services.RegisterHandlers(assembliesToScan);
        services.RegisterPipelineBehaviors(assembliesToScan);
        services.RegisterValidators(assembliesToScan);

        return services;
    }

    /// <summary>
    /// Rejestruje wszystkie handlery z podanych assemblies
    /// </summary>
    private static IServiceCollection RegisterHandlers(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            // Command Handlers bez wyniku (zwracające Result)
            var commandHandlers = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandHandler<>)))
                .ToList();

            foreach (var handlerType in commandHandlers)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(ICommandHandler<>));

                services.AddScoped(interfaceType, handlerType);

                // Log registration
                var commandType = interfaceType.GetGenericArguments()[0];
                Console.WriteLine($"Registered handler: {handlerType.Name} for command: {commandType.Name}");
            }

            // Command Handlers z wynikiem (zwracające Result<T>)
            var commandHandlersWithResult = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>)))
                .ToList();

            foreach (var handlerType in commandHandlersWithResult)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(ICommandHandler<,>));

                services.AddScoped(interfaceType, handlerType);

                var commandType = interfaceType.GetGenericArguments()[0];
                var resultType = interfaceType.GetGenericArguments()[1];
                Console.WriteLine($"Registered handler: {handlerType.Name} for command: {commandType.Name} returning: {resultType.Name}");
            }

            // Query Handlers (zawsze zwracają Result<T>)
            var queryHandlers = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                .ToList();

            foreach (var handlerType in queryHandlers)
            {
                var interfaceType = handlerType.GetInterfaces()
                    .First(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));

                services.AddScoped(interfaceType, handlerType);

                var queryType = interfaceType.GetGenericArguments()[0];
                var resultType = interfaceType.GetGenericArguments()[1];
                Console.WriteLine($"Registered handler: {handlerType.Name} for query: {queryType.Name} returning: {resultType.Name}");
            }
        }

        return services;
    }

    /// <summary>
    /// Rejestruje pipeline behaviors
    /// </summary>
    private static IServiceCollection RegisterPipelineBehaviors(this IServiceCollection services, Assembly[] assemblies)
    {
        // Rejestracja wbudowanych behaviors (kolejność ma znaczenie!)
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        // Rejestracja custom behaviors z assemblies
        foreach (var assembly in assemblies)
        {
            var behaviors = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IPipelineBehavior<,>)))
                .ToList();

            foreach (var behaviorType in behaviors)
            {
                // Pomiń już zarejestrowane wbudowane behaviors
                if (behaviorType.IsGenericType &&
                    (behaviorType.GetGenericTypeDefinition() == typeof(ValidationBehavior<,>)))
                {
                    continue;
                }

                services.AddScoped(typeof(IPipelineBehavior<,>), behaviorType);
                Console.WriteLine($"Registered behavior: {behaviorType.Name}");
            }
        }

        return services;
    }

    /// <summary>
    /// Rejestruje validators dla FluentValidation
    /// </summary>
    private static IServiceCollection RegisterValidators(this IServiceCollection services, Assembly[] assemblies)
    {
        foreach (var assembly in assemblies)
        {
            // Znajdź wszystkie typy implementujące IValidator<>
            var validatorTypes = assembly.GetTypes()
                .Where(t => !t.IsAbstract && !t.IsInterface && !t.IsGenericType)
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType &&
                    i.GetGenericTypeDefinition() == typeof(IValidator<>)))
                .ToList();

            foreach (var validatorType in validatorTypes)
            {
                // Znajdź wszystkie interfejsy IValidator<> które implementuje ten typ
                var validatorInterfaces = validatorType.GetInterfaces()
                    .Where(i => i.IsGenericType &&
                           i.GetGenericTypeDefinition() == typeof(IValidator<>))
                    .ToList();

                foreach (var validatorInterface in validatorInterfaces)
                {
                    // Rejestruj validator
                    services.AddScoped(validatorInterface, validatorType);

                    // Log dla debugowania
                    var modelType = validatorInterface.GetGenericArguments()[0];
                    Console.WriteLine($"Registered validator: {validatorType.Name} for: {modelType.Name}");
                }
            }
        }

        return services;
    }
}
