namespace NServiceBus.Unicast;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Pipeline;

/// <summary>
/// Maintains the message handlers for this endpoint.
/// </summary>
public class MessageHandlerRegistry
{
    /// <summary>
    /// Gets the list of handlers <see cref="Type" />s for the given
    /// <paramref name="messageType" />.
    /// </summary>
    public List<MessageHandler> GetHandlersFor(Type messageType)
    {
        ArgumentNullException.ThrowIfNull(messageType);

        var messageHandlers = new List<MessageHandler>();
        foreach (var handlersAndMessages in handlerAndMessagesHandledByHandlerCache)
        {
            var handlerType = handlersAndMessages.Key;
            foreach (var handlerDelegate in handlersAndMessages.Value)
            {
                if (handlerDelegate.MessageType.IsAssignableFrom(messageType))
                {
                    messageHandlers.Add(new MessageHandler(handlerDelegate.MethodDelegate, handlerType)
                    {
                        IsTimeoutHandler = handlerDelegate.IsTimeoutHandler
                    });
                }
            }
        }

        return messageHandlers;
    }

    internal IEnumerable<IMessageHandler> GetHandlersFor2(Type messageType) => allHandlers.Where(h => h.MessageType.IsAssignableFrom(messageType));

    /// <summary>
    /// Lists all message type for which we have handlers.
    /// </summary>
    /// <remarks>This method should not be called on a hot path.</remarks>
    public IEnumerable<Type> GetMessageTypes()
    {
        return (from messagesBeingHandled in handlerAndMessagesHandledByHandlerCache.Values
            from typeHandled in messagesBeingHandled
            let messageType = typeHandled.MessageType
            select messageType).Distinct();
    }

    /// <summary>
    /// Registers the given potential handler type.
    /// </summary>
    public void RegisterHandler(Type handlerType)
    {
        ArgumentNullException.ThrowIfNull(handlerType);

        if (handlerType.IsAbstract)
        {
            return;
        }

        ValidateHandlerType(handlerType);

        var messageTypes = GetMessageTypesBeingHandledBy(handlerType);

        foreach (var messageType in messageTypes)
        {
            if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(handlerType, out var typeList))
            {
                handlerAndMessagesHandledByHandlerCache[handlerType] = typeList = [];
            }

            CacheHandlerMethods(handlerType, messageType, typeList);
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear() => handlerAndMessagesHandledByHandlerCache.Clear();

    internal void Add(IMessageHandler messageHandler) => allHandlers.Add(messageHandler);

    static void CacheHandlerMethods(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
    {
        CacheMethod(handler, messageType, typeof(IHandleMessages<>), typeList, isTimeoutHandler: false);
        CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), typeList, isTimeoutHandler: true);
    }

    static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType,
        ICollection<DelegateHolder> methodList, bool isTimeoutHandler)
    {
        var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
        if (handleMethod == null)
        {
            return;
        }

        Log.DebugFormat("Associated '{0}' message with '{1}' handler.", messageType, handler);

        var delegateHolder = new DelegateHolder
        {
            MessageType = messageType, MethodDelegate = handleMethod, IsTimeoutHandler = isTimeoutHandler
        };
        methodList.Add(delegateHolder);
    }

    static Func<object, object, IMessageHandlerContext, Task> GetMethod(Type targetType, Type messageType,
        Type interfaceGenericType)
    {
        var interfaceType = interfaceGenericType.MakeGenericType(messageType);

        if (!interfaceType.IsAssignableFrom(targetType))
        {
            return null;
        }

        var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
        if (methodInfo == null)
        {
            return null;
        }

        var target = Expression.Parameter(typeof(object));
        var messageParam = Expression.Parameter(typeof(object));
        var contextParam = Expression.Parameter(typeof(IMessageHandlerContext));

        var castTarget = Expression.Convert(target, targetType);

        var methodParameters = methodInfo.GetParameters();
        var messageCastParam = Expression.Convert(messageParam, methodParameters.ElementAt(0).ParameterType);

        Expression body = Expression.Call(castTarget, methodInfo, messageCastParam, contextParam);

        return Expression
            .Lambda<Func<object, object, IMessageHandlerContext, Task>>(body, target, messageParam, contextParam)
            .CompileFast();
    }

    static Type[] GetMessageTypesBeingHandledBy(Type type)
    {
        return (from t in type.GetInterfaces()
                where t.IsGenericType
                let potentialMessageType = t.GetGenericArguments()[0]
                where
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                select potentialMessageType)
            .Distinct()
            .ToArray();
    }

    static void ValidateHandlerType(Type handlerType)
    {
        var propertyTypes = handlerType.GetProperties().Select(p => p.PropertyType).ToList();
        var ctorArguments = handlerType.GetConstructors()
            .SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType))
            .ToList();

        var dependencies = propertyTypes.Concat(ctorArguments).ToList();

        if (dependencies.Any(t => typeof(IMessageSession).IsAssignableFrom(t)))
        {
            throw new Exception(
                $"Interfaces IMessageSession or IEndpointInstance should not be resolved from the container to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the {handlerType.Name}.Handle method to send or publish messages.");
        }
    }

    readonly Dictionary<Type, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = [];
    readonly List<IMessageHandler> allHandlers = [];
    static readonly ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

    class DelegateHolder
    {
        public bool IsTimeoutHandler { get; set; }
        public Type MessageType;
        public Func<object, object, IMessageHandlerContext, Task> MethodDelegate;
    }


}

/// <summary>
///
/// </summary>
public static class EndpointConfigurationExtensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="endpointConfiguration"></param>
    /// <param name="register"></param>
    /// <typeparam name="THandler"></typeparam>
    /// <typeparam name="TMessage"></typeparam>
    public static void RegisterHandler<THandler, TMessage>(this EndpointConfiguration endpointConfiguration,
        bool register = false)
        where THandler : class, IHandleMessages<TMessage>
    {
        var registry = endpointConfiguration.Settings.GetOrCreate<MessageHandlerRegistry>();
        // or maybe all this should go into a generic method on the registry?
        registry.Add(new MessageHandler<THandler, TMessage>());

        if (register)
        {
            endpointConfiguration.RegisterComponents(s => s.AddScoped<THandler>());
        }
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="endpointConfiguration"></param>
    /// <param name="handlerType"></param>
    /// <param name="register"></param>
    public static void RegisterHandler(this EndpointConfiguration endpointConfiguration,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicMethods)]
        Type handlerType,
        bool register = false)
    {
        ValidateHandlerType(handlerType);

        foreach (var messageTypeBeingHandled in GetMessageTypesBeingHandledBy(handlerType))
        {
            RegisterHandlerForMessageType(endpointConfiguration, handlerType, messageTypeBeingHandled, register);
        }
    }

    static void RegisterHandlerForMessageType(EndpointConfiguration endpointConfiguration,
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                    DynamicallyAccessedMemberTypes.PublicMethods)]
        Type handlerType,
        Type messageType,
        bool register)
    {
        var genericMethod = typeof(EndpointConfigurationExtensions).GetMethod(nameof(RegisterHandler),
                                BindingFlags.Public | BindingFlags.Static,
                                [typeof(EndpointConfiguration), typeof(bool)])
                            ?? throw new InvalidOperationException(
                                $"Could not find generic {nameof(RegisterHandler)} method");

        var concreteMethod = genericMethod.MakeGenericMethod(handlerType, messageType);
        concreteMethod.Invoke(null, [endpointConfiguration, register]);
    }

    static Type[] GetMessageTypesBeingHandledBy(Type type) =>
    [
        .. (from t in type.GetInterfaces()
            where t.IsGenericType
            let potentialMessageType = t.GetGenericArguments()[0]
            where
                typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
            select potentialMessageType)
        .Distinct()
    ];

    static void ValidateHandlerType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors |
                                                                DynamicallyAccessedMemberTypes.PublicProperties)] Type handlerType)
    {
        var propertyTypes = handlerType.GetProperties().Select(p => p.PropertyType).ToList();
        var ctorArguments = handlerType.GetConstructors()
            .SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType))
            .ToList();

        var dependencies = propertyTypes.Concat(ctorArguments).ToList();

        if (dependencies.Any(t => typeof(IMessageSession).IsAssignableFrom(t)))
        {
            throw new Exception(
                $"Interfaces IMessageSession or IEndpointInstance should not be resolved from the container to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the {handlerType.Name}.Handle method to send or publish messages.");
        }
    }
}

interface IMessageHandler
{
    Type MessageType { get; }

    Task Handle(IServiceProvider provider, object message, IMessageHandlerContext context);
}

class MessageHandler<THandler, TMessage> : IMessageHandler
    where THandler : class, IHandleMessages<TMessage>
{
    static readonly ObjectFactory<THandler> handlerFactory;

    static MessageHandler() => handlerFactory = ActivatorUtilities.CreateFactory<THandler>([]);

    public Type MessageType { get; } = typeof(TMessage);

    public Task Handle(IServiceProvider provider, object message, IMessageHandlerContext context)
    {
        // we probably have to seperate the create from the handle how core operates to day and somehow make it backward compatible with MessageHandler
        var handler = handlerFactory(provider, []);
        return handler.Handle((TMessage)message, context);
    }
}