namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using NServiceBus.Logging;
    using NServiceBus.Unicast.Behaviors;

    /// <summary>
    ///     Maintains the message handlers for this endpoint.
    /// </summary>
    public partial class MessageHandlerRegistry
    {
        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();
        readonly Conventions conventions;
        readonly IDictionary<RuntimeTypeHandle, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();

        internal MessageHandlerRegistry(Conventions conventions)
        {
            this.conventions = conventions;
        }

        /// <summary>
        ///     Gets the list of handlers <see cref="Type" />s for the given
        ///     <paramref name="messageType" />.
        /// </summary>
        public IEnumerable<MessageHandler> GetHandlersFor(Type messageType)
        {
            Guard.AgainstNull("messageType", messageType);
            if (!conventions.IsMessageType(messageType))
            {
                return Enumerable.Empty<MessageHandler>();
            }

            return from handlersAndMessages in handlerAndMessagesHandledByHandlerCache
                   from messagesBeingHandled in handlersAndMessages.Value
                   where Type.GetTypeFromHandle(messagesBeingHandled.MessageType).IsAssignableFrom(messageType)
                   select new MessageHandler(messagesBeingHandled.MethodDelegate, Type.GetTypeFromHandle(handlersAndMessages.Key));
        }

        /// <summary>
        ///     Lists all message type for which we have handlers.
        /// </summary>
        public IEnumerable<Type> GetMessageTypes()
        {
            return (from messagesBeingHandled in handlerAndMessagesHandledByHandlerCache.Values
                    from typeHandled in messagesBeingHandled
                    let messageType = Type.GetTypeFromHandle(typeHandled.MessageType)
                    where conventions.IsMessageType(messageType)
                    select messageType).Distinct();
        }

        /// <summary>
        ///     Registers the given potentialHandlerKind handler type.
        /// </summary>
        public void RegisterHandler(Type handlerType)
        {
            Guard.AgainstNull("handlerType", handlerType);
            if (handlerType.IsAbstract)
            {
                return;
            }

            var messageTypes = GetMessageTypesBeingHandledBy(handlerType);

            foreach (var messageType in messageTypes)
            {
                List<DelegateHolder> typeList;
                var typeHandle = handlerType.TypeHandle;
                if (!handlerAndMessagesHandledByHandlerCache.TryGetValue(typeHandle, out typeList))
                {
                    handlerAndMessagesHandledByHandlerCache[typeHandle] = typeList = new List<DelegateHolder>();
                }

                CacheHandlerMethods(handlerType, messageType, typeList);
            }
        }

        /// <summary>
        ///     Clears the cache.
        /// </summary>
        public void Clear()
        {
            handlerAndMessagesHandledByHandlerCache.Clear();
        }

        static void CacheHandlerMethods(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), typeList);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), typeList);
        }

        static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, ICollection<DelegateHolder> methodList)
        {
            var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
            if (handleMethod == null)
            {
                return;
            }
            Log.DebugFormat("Associated '{0}' message with '{1}' handler.", messageType, handler);

            var delegateHolder = new DelegateHolder
            {
                MessageType = messageType.TypeHandle,
                MethodDelegate = handleMethod
            };
            methodList.Add(delegateHolder);
        }

        static Action<object, object> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
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

            var castTarget = Expression.Convert(target, targetType);

            var methodParameters = methodInfo.GetParameters();
            var messageCastParam = Expression.Convert(messageParam, methodParameters.ElementAt(0).ParameterType);

            //var contextParameter = methodParameters.ElementAtOrDefault(1);
            //if (IsNewContextApi(contextParameter))
            //{
            //    var contextType = contextParameter.ParameterType;
            //    var contextCastParam = Expression.Convert(contextParam, contextType);
            //    var execute = Expression.Call(castTarget, methodInfo, messageCastParam, contextCastParam);
            //    return Expression.Lambda<Action<object, object, object>>(execute, target, messageParam, contextParam).Compile();
            //}

            var innerExecute = Expression.Call(castTarget, methodInfo, messageCastParam);
            return Expression.Lambda<Action<object, object>>(innerExecute, target, messageParam).Compile();
        }

        static IEnumerable<Type> GetMessageTypesBeingHandledBy(Type type)
        {
            return (from t in type.GetInterfaces()
                    where t.IsGenericType
                    let potentialMessageType = t.GetGenericArguments()[0]
                    where
                        typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                    select potentialMessageType)
                   .Distinct()
                   .ToList();
        }

        class DelegateHolder
        {
            public RuntimeTypeHandle MessageType;
            public Action<object, object> MethodDelegate;
        }
    }
}