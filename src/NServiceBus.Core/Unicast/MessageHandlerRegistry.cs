namespace NServiceBus.Unicast;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using FastExpressionCompiler;
using Logging;
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
                    messageHandlers.Add(new MessageHandler(handlerDelegate.MethodDelegate, handlerType)
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
    public void RegisterHandlerForMessage<THandler, TMessage>() where THandler : IHandleMessages<TMessage>
    {
        if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(typeof(THandler), out var typeList))
        {
            handlerAndMessagesHandledByHandlerCache[typeof(THandler)] = typeList = [];
        }

        CacheMethod<THandler, TMessage>(typeof(IHandleMessages<>), typeList, isTimeoutHandler: false);
        CacheMethod<THandler, TMessage>(typeof(IHandleTimeouts<>), typeList, isTimeoutHandler: true);
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

    static void CacheMethod<THandler, TMessage>(Type interfaceGenericType, ICollection<DelegateHolder> methodList, bool isTimeoutHandler)
    {
        var handler = typeof(THandler);
        var messageType = typeof(TMessage);
        var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
        if (handleMethod == null)
        {
            return;
        }
        Log.DebugFormat("Associated '{0}' message with '{1}' handler.", messageType, handler);

        var delegateHolder = new DelegateHolder
        {
            MessageType = messageType,
            MethodDelegate = handleMethod,
            IsTimeoutHandler = isTimeoutHandler
        };
        methodList.Add(delegateHolder);
    }

    static Func<object, object, IMessageHandlerContext, Task> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
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

        return Expression.Lambda<Func<object, object, IMessageHandlerContext, Task>>(body, target, messageParam, contextParam).CompileFast();
    }

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

    readonly Dictionary<Type, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = [];
    static readonly Type IHandleMessagesType = typeof(IHandleMessages<>);
    static readonly ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

    class DelegateHolder
    {
        public bool IsTimeoutHandler { get; set; }
        public Type MessageType;
        public Func<object, object, IMessageHandlerContext, Task> MethodDelegate;
    }
}