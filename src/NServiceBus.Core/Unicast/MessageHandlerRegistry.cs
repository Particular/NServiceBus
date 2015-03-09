namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using NServiceBus.Logging;
    using NServiceBus.Saga;
    using NServiceBus.Unicast.Behaviors;

    /// <summary>
    ///     Maintains the message handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry
    {
        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();
        readonly Conventions conventions;
        readonly Dictionary<RuntimeTypeHandle, List<DelegateHolder>> handlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();
        readonly IDictionary<RuntimeTypeHandle, List<Type>> handlerList = new Dictionary<RuntimeTypeHandle, List<Type>>();

        internal MessageHandlerRegistry(Conventions conventions)
        {
            this.conventions = conventions;
        }

        /// <summary>
        ///     Gets the list of <see cref="IHandleMessages{T}" /> <see cref="Type" />s for the given
        ///     <paramref name="messageType" />
        /// </summary>
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.GetHandlersFor(Type messageType)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Gets the list of handlers <see cref="Type" />s for the given
        ///     <paramref name="messageType" />
        /// </summary>
        public IEnumerable<MessageHandler> GetHandlersFor(Type messageType)
        {
            if (!conventions.IsMessageType(messageType))
            {
                return Enumerable.Empty<MessageHandler>();
            }

            return from keyValue in handlerList
                where keyValue.Value.Any(msgTypeHandled => msgTypeHandled.IsAssignableFrom(messageType))
                select new MessageHandler(Invoke, Type.GetTypeFromHandle(keyValue.Key));
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
            if (handlerType.IsAbstract)
            {
                return;
            }

            var messageTypes = GetMessageTypesIfIsMessageHandler(handlerType).ToList();

            foreach (var messageType in messageTypes)
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
        /// <param name="context">The context instance.</param>
        public void Invoke(object handler, object message, object context)
        {
            Invoke(handler, message, context, handlerCache);
        }

        /// <summary>
        ///     Invokes the handle method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="message">The message instance.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.Invoke(object handler, object message, object context)" , RemoveInVersion = "7" , TreatAsErrorFromVersion = "6")]
        public void InvokeHandle(object handler, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Invokes the timeout method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="state">The message instance.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.Invoke(object handler, object message, object context)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void InvokeTimeout(object handler, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Clears the cache
        /// </summary>
        public void Clear()
        {
            handlerCache.Clear();
        }

        void CacheMethodForHandler(Type handler, Type messageType)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), handlerCache);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), handlerCache);
            CacheMethod(handler, messageType, typeof(IHandleTimeout<>), handlerCache);
            CacheMethod(handler, messageType, typeof(IHandle<>), handlerCache);
            CacheMethod(handler, messageType, typeof(ISubscribe<>), handlerCache);
        }

        static void Invoke(object handler, object message, object context, Dictionary<RuntimeTypeHandle, List<DelegateHolder>> dictionary)
        {
            List<DelegateHolder> methodList;
            if (!dictionary.TryGetValue(handler.GetType().TypeHandle, out methodList))
            {
                return;
            }
            foreach (var delegateHolder in methodList.Where(x => x.MessageType.IsInstanceOfType(message)))
            {
                delegateHolder.MethodDelegate(handler, message, context);
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

        static Action<object, object, object> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
        {
            var interfaceType = interfaceGenericType.MakeGenericType(messageType);

            if (interfaceType.IsAssignableFrom(targetType))
            {
                var methodInfo = targetType.GetInterfaceMap(interfaceType).TargetMethods.FirstOrDefault();
                if (methodInfo != null)
                {
                    var target = Expression.Parameter(typeof(object));
                    var messageParam = Expression.Parameter(typeof(object));
                    var contextParam = Expression.Parameter(typeof(object));

                    var castTarget = Expression.Convert(target, targetType);

                    var methodParameters = methodInfo.GetParameters();
                    var messageCastParam = Expression.Convert(messageParam, methodParameters.ElementAt(0).ParameterType);

                    var contextParameter = methodParameters.ElementAtOrDefault(1);
                    if (IsNewContextApi(contextParameter))
                    {
                        var contextCastParam = Expression.Convert(contextParam, contextParameter.ParameterType);
                        var execute = Expression.Call(castTarget, methodInfo, messageCastParam, contextCastParam);
                        return Expression.Lambda<Action<object, object, object>>(execute, target, messageParam, contextParam).Compile();
                    }
                    var innerExecute = Expression.Call(castTarget, methodInfo, messageCastParam);
                    return Expression.Lambda<Action<object, object, object>>(innerExecute, target, messageParam, contextParam).Compile();
                }
            }

            return null;
        }

        static bool IsNewContextApi(ParameterInfo contextParameter)
        {
            return contextParameter != null;
        }

        static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            return from t in type.GetInterfaces()
                where t.IsGenericType
                let potentialMessageType = t.GetGenericArguments()[0]
                where
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandleTimeout<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandle<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(ISubscribe<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)

                   select potentialMessageType;
        }

        class DelegateHolder
        {
            public Type MessageType;
            public Action<object, object, object> MethodDelegate;
        }
    }
}