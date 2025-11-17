namespace NServiceBus.Unicast;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
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
        foreach (var handlersAndMessages in messageHandlerFactories)
        {
            foreach (var handlerFactory in handlersAndMessages.Value)
            {
                if (handlerFactory.MessageType.IsAssignableFrom(messageType))
                {
                    messageHandlers.Add(handlerFactory.Create());
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
        return (from messagesBeingHandled in messageHandlerFactories.Values
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
        ReplacementTypeOrMember = "AddHandler<THandler>()")]
    [Obsolete("Deprecated in favor of a strongly-typed alternative. Use 'AddHandler<THandler>()' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public void RegisterHandler(Type handlerType) => AddHandlerWithReflection(handlerType);

    void AddHandlerWithReflection(Type handlerType) =>
        typeof(MessageHandlerRegistry)
            .GetMethod(nameof(AddHandler), BindingFlags.Public | BindingFlags.Instance, [])!
            .MakeGenericMethod(handlerType)
            .Invoke(this, []);

    /// <summary>
    /// Registers the given potential handler type.
    /// </summary>
    public void AddHandler<THandler>() where THandler : IHandleMessages
    {
        var handlerType = typeof(THandler);

        if (handlerType.IsAbstract)
        {
            return;
        }

        foreach (var interfaceType in handlerType.GetInterfaces())
        {
            if (!interfaceType.IsGenericType)
            {
                continue;
            }

            var genericTypeDefinition = interfaceType.GetGenericTypeDefinition();
            var messageType = interfaceType.GetGenericArguments()[0];
            if (genericTypeDefinition == typeof(IHandleMessages<>))
            {
                AddMessageHandlerForMessageMethodInfo.MakeGenericMethod(handlerType, messageType)
                    .Invoke(this, []);
            }

            if (genericTypeDefinition == typeof(IHandleTimeouts<>))
            {
                AddTimeoutHandlerForMessageMethodInfo.MakeGenericMethod(handlerType, messageType)
                    .Invoke(this, []);
            }
        }
    }

    /// <summary>
    /// Add a handler for a specific message type. Should only be called by a source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddMessageHandlerForMessage<THandler, TMessage>() where THandler : class, IHandleMessages<TMessage>
    {
        Log.DebugFormat("Associated '{0}' message with '{1}' message handler.", typeof(TMessage), typeof(THandler));
        AddHandlerFactory<THandler>(new MessageHandlerFactory<THandler, TMessage>());
    }

    /// <summary>
    /// Add a handler for a specific timeout type. Should only be called by a source generator.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddTimeoutHandlerForMessage<THandler, TMessage>() where THandler : class, IHandleTimeouts<TMessage>
    {
        Log.DebugFormat("Associated '{0}' message with '{1}' timeout handler.", typeof(TMessage), typeof(THandler));
        AddHandlerFactory<THandler>(new TimeoutHandlerFactory<THandler, TMessage>());
    }

    void AddHandlerFactory<THandler>(IMessageHandlerFactory handlerFactory)
    {
        if (!messageHandlerFactories.TryGetValue(typeof(THandler), out var handlerFactories))
        {
            messageHandlerFactories[typeof(THandler)] = handlerFactories = [];
        }

        handlerFactories.Add(handlerFactory);
    }

    static readonly MethodInfo AddMessageHandlerForMessageMethodInfo = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(AddMessageHandlerForMessage)) ?? throw new MissingMethodException(nameof(AddMessageHandlerForMessage));

    static readonly MethodInfo AddTimeoutHandlerForMessageMethodInfo = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(AddTimeoutHandlerForMessage)) ?? throw new MissingMethodException(nameof(AddTimeoutHandlerForMessage));


    /// <summary>
    /// Add handlers from types scanned at runtime.
    /// </summary>
    /// <param name="orderedTypes">Scanned types, with "load handlers first" types ordered first.</param>
    public void AddScannedHandlers(IEnumerable<Type> orderedTypes)
    {
        foreach (var type in orderedTypes.Where(IsMessageHandler))
        {
            AddHandlerWithReflection(type);
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
    public void Clear() => messageHandlerFactories.Clear();

    readonly Dictionary<Type, List<IMessageHandlerFactory>> messageHandlerFactories = [];
    static readonly Type IHandleMessagesType = typeof(IHandleMessages<>);
    static readonly ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

    interface IMessageHandlerFactory
    {
        Type MessageType { get; }
        MessageHandler Create();
    }

    sealed class TimeoutHandlerFactory<THandler, TMessage> : IMessageHandlerFactory
        where THandler : class
    {
        public Type MessageType { get; } = typeof(TMessage);

        public MessageHandler Create() =>
            new MessageHandlerInvoker<IHandleTimeouts<TMessage>, TMessage>(
                static provider => handlerFactory(provider, []),
                static (handler, message, handlerContext) => handler.Timeout(message, handlerContext),
                isTimeoutHandler: true)
            {
                HandlerType = typeof(THandler)
            };

        static readonly ObjectFactory<THandler> factory = ActivatorUtilities.CreateFactory<THandler>([]);

        static readonly ObjectFactory<IHandleTimeouts<TMessage>> handlerFactory =
            static (sp, args) => (IHandleTimeouts<TMessage>)factory(sp, args);
    }

    sealed class MessageHandlerFactory<THandler, TMessage> : IMessageHandlerFactory
        where THandler : class
    {
        public Type MessageType { get; } = typeof(TMessage);

        public MessageHandler Create() =>
            new MessageHandlerInvoker<IHandleMessages<TMessage>, TMessage>(
                static provider => handlerFactory(provider, []),
                static (handler, message, handlerContext) => handler.Handle(message, handlerContext),
                isTimeoutHandler: false)
            {
                HandlerType = typeof(THandler)
            };

        static readonly ObjectFactory<THandler> factory = ActivatorUtilities.CreateFactory<THandler>([]);

        static readonly ObjectFactory<IHandleMessages<TMessage>> handlerFactory =
            static (sp, args) => (IHandleMessages<TMessage>)factory(sp, args);
    }
}