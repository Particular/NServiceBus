namespace NServiceBus.Unicast;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using Particular.Obsoletes;
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
                    messageHandlers.Add(new MessageHandler(handlerDelegate.CreateHandler, handlerDelegate.Handle, handlerType)
                    {
                        IsTimeoutHandler = handlerDelegate.IsTimeoutHandler
                    });
                }
            }
        }
        return messageHandlers;
    }

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
    [ObsoleteMetadata(Message = "Deprecated in favor of a strongly-typed alternative",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "RegisterHandler<THandler>()")]
    [Obsolete("Deprecated in favor of a strongly-typed alternative. Use 'RegisterHandler<THandler>()' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public void RegisterHandler(Type handlerType) => RegisterHandlerWithReflection(handlerType);

    void RegisterHandlerWithReflection(Type handlerType) =>
        typeof(MessageHandlerRegistry)
            .GetMethod(nameof(RegisterHandler), BindingFlags.Public | BindingFlags.Instance, [])!
            .MakeGenericMethod(handlerType)
            .Invoke(this, []);

    /// <summary>
    /// Registers the given potential handler type.
    /// </summary>
    public void RegisterHandler<THandler>() where THandler : IHandleMessages
    {
        var handlerType = typeof(THandler);

        if (handlerType.IsAbstract)
        {
            return;
        }

        var messageTypes = GetMessageTypesBeingHandledBy(handlerType);

        foreach (var messageType in messageTypes)
        {
            RegisterHandlerForMessageMethodInfo.MakeGenericMethod(handlerType, messageType)
                .Invoke(this, []);
        }
    }

    /// <summary>
    /// Register a handler for a specific message type. Should only be called by a source generator.
    /// </summary>
    public void RegisterHandlerForMessage<THandler, TMessage>() where THandler : class
    {
        if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(typeof(THandler), out var methodList))
        {
            handlerAndMessagesHandledByHandlerCache[typeof(THandler)] = methodList = [];
        }

        Log.DebugFormat("Associated '{0}' message with '{1}' handler.", typeof(TMessage), typeof(THandler));
        methodList.Add(new DelegateHolder<THandler, TMessage> { IsTimeoutHandler = false });
        methodList.Add(new DelegateHolder<THandler, TMessage> { IsTimeoutHandler = true });
    }

    static readonly MethodInfo RegisterHandlerForMessageMethodInfo = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(RegisterHandlerForMessage)) ?? throw new MissingMethodException("RegisterHandlerForMessage");

    /// <summary>
    /// Add handlers from types scanned at runtime.
    /// </summary>
    /// <param name="orderedTypes">Scanned types, with "load handlers first" types ordered first.</param>
    public void AddScannedHandlers(IEnumerable<Type> orderedTypes)
    {
        foreach (var type in orderedTypes.Where(IsMessageHandler))
        {
            RegisterHandlerWithReflection(type);
        }
    }

    internal static bool IsMessageHandler(Type type)
    {
        if (type.IsAbstract || type.IsGenericTypeDefinition)
        {
            return false;
        }

        return type.GetInterfaces()
            .Where(@interface => @interface.IsGenericType)
            .Select(@interface => @interface.GetGenericTypeDefinition())
            .Any(genericTypeDef => genericTypeDef == IHandleMessagesType);
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear() => handlerAndMessagesHandledByHandlerCache.Clear();

    static Type[] GetMessageTypesBeingHandledBy(Type type) =>
        type.GetInterfaces()
            .Where(t =>
            {
                if (!t.IsGenericType)
                {
                    return false;
                }

                var genericTypeDefinition = t.GetGenericTypeDefinition();
                return genericTypeDefinition == typeof(IHandleMessages<>) || genericTypeDefinition == typeof(IHandleTimeouts<>);
            })
            .Select(t => t.GetGenericArguments()[0])
            .Distinct()
            .ToArray();

    readonly Dictionary<Type, List<IDelegateHolder>> handlerAndMessagesHandledByHandlerCache = [];
    static readonly Type IHandleMessagesType = typeof(IHandleMessages<>);
    static readonly ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

    interface IDelegateHolder
    {
        Type MessageType { get; }
        bool IsTimeoutHandler { get; init; }
        object CreateHandler(IServiceProvider provider);
        Task Handle(object handler, object message, IMessageHandlerContext context);
    }

    class DelegateHolder<THandler, TMessage> : IDelegateHolder
        where THandler : class
    {
        public DelegateHolder()
        {
            foreach (var iface in typeof(THandler).GetInterfaces())
            {
                if (iface.IsGenericType && iface.IsAssignableTo(typeof(IHandleTimeouts<TMessage>)))
                {
                    IsTimeoutHandler = true;
                    return;
                }
            }
        }

        public Type MessageType { get; } = typeof(TMessage);

        public bool IsTimeoutHandler { get; init; }

        public object CreateHandler(IServiceProvider provider) => handlerFactory(provider, []);

        public Task Handle(object handler, object message, IMessageHandlerContext context)
        {
            if (IsTimeoutHandler && handler is IHandleTimeouts<TMessage> timeoutHandler)
            {
                return timeoutHandler.Timeout((TMessage)message, context);
            }

            if (!IsTimeoutHandler && handler is IHandleMessages<TMessage> messageHandler)
            {
                return messageHandler.Handle((TMessage)message, context);
            }

            return Task.CompletedTask;
        }

        static readonly ObjectFactory<THandler> handlerFactory = ActivatorUtilities.CreateFactory<THandler>([]);
    }
}