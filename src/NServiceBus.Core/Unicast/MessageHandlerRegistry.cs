namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using NServiceBus.Logging;
    using NServiceBus.Saga;

    /// <summary>
    ///     Maintains the message handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry
    {
        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();
        readonly Conventions conventions;
        readonly Dictionary<RuntimeTypeHandle, List<DelegateHolder>> handlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();
        readonly IDictionary<RuntimeTypeHandle, List<Type>> handlerList = new Dictionary<RuntimeTypeHandle, List<Type>>();
        readonly Dictionary<RuntimeTypeHandle, List<DelegateHolder>> timeoutCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();

        internal MessageHandlerRegistry(Conventions conventions)
        {
            this.conventions = conventions;
        }

        /// <summary>
        ///     Gets the list of <see cref="IHandleMessages{T}" /> <see cref="Type" />s for the given
        ///     <paramref name="messageType" />
        /// </summary>
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            Guard.AgainstNull(messageType, "messageType");
            if (!conventions.IsMessageType(messageType))
            {
                return Enumerable.Empty<Type>();
            }

            return from keyValue in handlerList
                where keyValue.Value.Any(msgTypeHandled => msgTypeHandled.IsAssignableFrom(messageType))
                select Type.GetTypeFromHandle(keyValue.Key);
        }

        /// <summary>
        ///     Lists all message type for which we have handlers
        /// </summary>
        public IEnumerable<Type> GetMessageTypes()
        {
            return from handlers in handlerList.Values
                from typeHandled in handlers
                where conventions.IsMessageType(typeHandled)
                select typeHandled;
        }

        /// <summary>
        ///     Registers the given message handler type
        /// </summary>
        public void RegisterHandler(Type handlerType)
        {
            Guard.AgainstNull(handlerType, "handlerType");
            if (handlerType.IsAbstract)
            {
                return;
            }

            var messageTypesThisHandlerHandles = GetMessageTypesIfIsMessageHandler(handlerType).ToList();

            foreach (var messageType in messageTypesThisHandlerHandles)
            {
                List<Type> typeList;
                var typeHandle = handlerType.TypeHandle;
                if (!handlerList.TryGetValue(typeHandle, out typeList))
                {
                    handlerList[typeHandle] = typeList = new List<Type>();
                }

                if (!typeList.Contains(messageType))
                {
                    typeList.Add(messageType);
                    Log.DebugFormat("Associated '{0}' message with '{1}' handler", messageType, handlerType);
                }

                CacheMethodForHandler(handlerType, messageType);
            }
        }

        /// <summary>
        ///     Invokes the handle method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="message">The message instance.</param>
        public void InvokeHandle(object handler, object message)
        {
            Guard.AgainstNull(handler, "handler");
            Guard.AgainstNull(message, "message");
            Invoke(handler, message, handlerCache);
        }

        /// <summary>
        ///     Invokes the timeout method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="state">The message instance.</param>
        public void InvokeTimeout(object handler, object state)
        {
            Guard.AgainstNull(handler, "handler");
            Guard.AgainstNull(state, "state");
            Invoke(handler, state, timeoutCache);
        }

        /// <summary>
        ///     Registers the method in the cache
        /// </summary>
        /// <param name="handler">The object type.</param>
        /// <param name="messageType">the message type.</param>
        public void CacheMethodForHandler(Type handler, Type messageType)
        {
            Guard.AgainstNull(handler, "handler");
            Guard.AgainstNull(messageType, "messageType");
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), handlerCache);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), timeoutCache);
        }

        /// <summary>
        ///     Clears the cache
        /// </summary>
        public void Clear()
        {
            handlerCache.Clear();
            timeoutCache.Clear();
        }

        static void Invoke(object handler, object message, Dictionary<RuntimeTypeHandle, List<DelegateHolder>> dictionary)
        {
            List<DelegateHolder> methodList;
            if (!dictionary.TryGetValue(handler.GetType().TypeHandle, out methodList))
            {
                return;
            }
            foreach (var delegateHolder in methodList.Where(x => x.MessageType.IsInstanceOfType(message)))
            {
                delegateHolder.MethodDelegate(handler, message);
            }
        }

        static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, Dictionary<RuntimeTypeHandle, List<DelegateHolder>> cache)
        {
            var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
            if (handleMethod == null)
            {
                return;
            }
            var delegateHolder = new DelegateHolder
            {
                MessageType = messageType,
                MethodDelegate = handleMethod
            };
            List<DelegateHolder> methodList;
            if (cache.TryGetValue(handler.TypeHandle, out methodList))
            {
                if (methodList.Any(x => x.MessageType == messageType))
                {
                    return;
                }
                methodList.Add(delegateHolder);
            }
            else
            {
                cache[handler.TypeHandle] = new List<DelegateHolder>
                {
                    delegateHolder
                };
            }
        }

        static Action<object, object> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
        {
            var interfaceType = interfaceGenericType.MakeGenericType(messageType);

            if (interfaceType.IsAssignableFrom(targetType))
            {
                var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
                if (methodInfo != null)
                {
                    var target = Expression.Parameter(typeof(object));
                    var param = Expression.Parameter(typeof(object));

                    var castTarget = Expression.Convert(target, targetType);
                    var castParam = Expression.Convert(param, methodInfo.GetParameters().First().ParameterType);
                    var execute = Expression.Call(castTarget, methodInfo, castParam);
                    return Expression.Lambda<Action<object, object>>(execute, target, param).Compile();
                }
            }

            return null;
        }

        static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            return from t in type.GetInterfaces()
                where t.IsGenericType
                let potentialMessageType = t.GetGenericArguments()[0]
                where
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                select potentialMessageType;
        }

        class DelegateHolder
        {
            public Type MessageType;
            public Action<object, object> MethodDelegate;
        }
    }
}