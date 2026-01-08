namespace NServiceBus.Unicast;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
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
    /// Gets the list of message handlers for the given message type.
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

    /// <summary>
    /// Registers the handler type.
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
                _ = AddMessageHandlerForMessageMethod.InvokeGeneric(this, [handlerType, messageType]);
            }

            if (genericTypeDefinition == typeof(IHandleTimeouts<>))
            {
                _ = AddTimeoutHandlerForMessageMethod.InvokeGeneric(this, [handlerType, messageType]);
            }
        }
    }

    /// <summary>
    /// Add a handler for a specific message type. Should only be called by a source generator.
    /// </summary>
    public void AddMessageHandlerForMessage<THandler, TMessage>() where THandler : class, IHandleMessages<TMessage>
    {
        // We are keeping a small deduplication set to avoid registering the same handler+message combination multiple times
        // and are using a factory to avoid allocation the IMessageHandlerFactory unless it's needed since it can be expensive
        if (!deduplicationSet.Add(HandlerAndMessage.New<THandler, TMessage>()))
        {
            return;
        }

        Log.DebugFormat("Associated '{0}' message with '{1}' message handler.", typeof(TMessage), typeof(THandler));
        var handlerFactories = GetOrCreate<THandler>();
        handlerFactories.Add(new MessageHandlerFactory<THandler, TMessage>());
    }

    /// <summary>
    /// Add a handler for a specific timeout type. Should only be called by a source generator.
    /// </summary>
    public void AddTimeoutHandlerForMessage<THandler, TMessage>() where THandler : class, IHandleTimeouts<TMessage>
    {
        // We are keeping a small deduplication set to avoid registering the same handler+message combination multiple times
        // and are using a factory to avoid allocation the IMessageHandlerFactory unless it's needed since it can be expensive
        if (!deduplicationSet.Add(HandlerAndMessage.New<THandler, TMessage>(isTimeoutHandler: true)))
        {
            return;
        }

        Log.DebugFormat("Associated '{0}' message with '{1}' timeout handler.", typeof(TMessage), typeof(THandler));
        var handlerFactories = GetOrCreate<THandler>();
        handlerFactories.Add(new TimeoutHandlerFactory<THandler, TMessage>());
    }

    List<IMessageHandlerFactory> GetOrCreate<THandler>()
    {
        if (!messageHandlerFactories.TryGetValue(typeof(THandler), out var handlerFactories))
        {
            messageHandlerFactories[typeof(THandler)] = handlerFactories = [];
        }

        return handlerFactories;
    }

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
    public void Clear()
    {
        messageHandlerFactories.Clear();
        deduplicationSet.Clear();
    }

    void AddHandlerWithReflection(Type handlerType) =>
        AddHandlerWithReflectionMethod.InvokeGeneric(this, [handlerType]);

    static readonly MethodInfo AddHandlerWithReflectionMethod = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(AddHandler), BindingFlags.Public | BindingFlags.Instance, []) ?? throw new MissingMethodException(nameof(AddHandler));

    static readonly MethodInfo AddMessageHandlerForMessageMethod = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(AddMessageHandlerForMessage)) ?? throw new MissingMethodException(nameof(AddMessageHandlerForMessage));

    static readonly MethodInfo AddTimeoutHandlerForMessageMethod = typeof(MessageHandlerRegistry)
        .GetMethod(nameof(AddTimeoutHandlerForMessage)) ?? throw new MissingMethodException(nameof(AddTimeoutHandlerForMessage));


    readonly Dictionary<Type, List<IMessageHandlerFactory>> messageHandlerFactories = [];
    readonly HashSet<HandlerAndMessage> deduplicationSet = [];
    static readonly Type IHandleMessagesType = typeof(IHandleMessages<>);
    static readonly ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

    readonly record struct HandlerAndMessage(Type HandlerType, Type MessageType, bool IsTimeoutHandler)
    {
        public static HandlerAndMessage New<THandler, TMessage>(bool isTimeoutHandler = false) =>
            new(HandlerType: typeof(THandler), MessageType: typeof(TMessage), IsTimeoutHandler: isTimeoutHandler);
    }

    interface IMessageHandlerFactory
    {
        Type MessageType { get; }
        MessageHandler Create();
    }

    // Be cautious when renaming this class because it is used in Core acceptance tests to verify it is hidden from the stack traces
    sealed class TimeoutHandlerFactory<THandler, TMessage> : IMessageHandlerFactory
        where THandler : class
    {
        public Type MessageType { get; } = typeof(TMessage);

        public MessageHandler Create() =>
            new MessageHandlerInvoker<IHandleTimeouts<TMessage>, TMessage>(
                static provider => handlerFactory(provider, []),
                InvokeTimeout, // This needs to stay a method group to wipe it out from the stack trace
                isTimeoutHandler: true)
            {
                HandlerType = typeof(THandler)
            };

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [StackTraceHidden]
        static Task InvokeTimeout(IHandleTimeouts<TMessage> handler, TMessage message, IMessageHandlerContext handlerContext) => handler.Timeout(message, handlerContext);

        static readonly ObjectFactory<THandler> factory = ActivatorUtilities.CreateFactory<THandler>([]);

        static readonly ObjectFactory<IHandleTimeouts<TMessage>> handlerFactory =
            static (sp, args) => Unsafe.As<IHandleTimeouts<TMessage>>(factory(sp, args));
    }

    // Be cautious when renaming this class because it is used in Core acceptance tests to verify it is hidden from the stack traces
    sealed class MessageHandlerFactory<THandler, TMessage> : IMessageHandlerFactory
        where THandler : class
    {
        public Type MessageType { get; } = typeof(TMessage);

        public MessageHandler Create() =>
            new MessageHandlerInvoker<IHandleMessages<TMessage>, TMessage>(
                static provider => handlerFactory(provider, []),
                InvokeHandler, // This needs to stay a method group to wipe it out from the stack trace
                isTimeoutHandler: false)
            {
                HandlerType = typeof(THandler)
            };

        [DebuggerNonUserCode]
        [DebuggerStepThrough]
        [StackTraceHidden]
        static Task InvokeHandler(IHandleMessages<TMessage> handler, TMessage message, IMessageHandlerContext handlerContext) => handler.Handle(message, handlerContext);

        static readonly ObjectFactory<THandler> factory = ActivatorUtilities.CreateFactory<THandler>([]);

        static readonly ObjectFactory<IHandleMessages<TMessage>> handlerFactory =
            static (sp, args) => Unsafe.As<IHandleMessages<TMessage>>(factory(sp, args));
    }
}