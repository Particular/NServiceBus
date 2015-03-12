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
    ///     Maintains the potentialHandlerKind handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry
    {
        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();
        readonly Conventions conventions;
        readonly IDictionary<RuntimeTypeHandle, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();

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

            return from handlersAndMessages in handlerAndMessagesHandledByHandlerCache
                from messagesBeingHandled in handlersAndMessages.Value
                where Type.GetTypeFromHandle(messagesBeingHandled.MessageType).IsAssignableFrom(messageType)
                select new MessageHandler(messagesBeingHandled.MethodDelegate, Type.GetTypeFromHandle(handlersAndMessages.Key), messagesBeingHandled.HandlerKind);
        }

        /// <summary>
        ///     Lists all potentialHandlerKind type for which we have handlers
        /// </summary>
        public IEnumerable<Type> GetMessageTypes()
        {
            return from messagesBeingHandled in handlerAndMessagesHandledByHandlerCache.Values
                   from typeHandled in messagesBeingHandled
                   let messageType = Type.GetTypeFromHandle(typeHandled.MessageType)
                   where conventions.IsMessageType(messageType)
                   select messageType; // Daniel: Distinct??
        }

        /// <summary>
        ///     Registers the given potentialHandlerKind handler type
        /// </summary>
        public void RegisterHandler(Type handlerType)
        {
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

                CacheMethodForHandler(handlerType, messageType, typeList);
            }
        }

        /// <summary>
        ///     Invokes the handle method of the given handler passing the potentialHandlerKind
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="message">The potentialHandlerKind instance.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.Invoke(object handler, object potentialHandlerKind, object context)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void InvokeHandle(object handler, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Invokes the timeout method of the given handler passing the potentialHandlerKind
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="state">The potentialHandlerKind instance.</param>
        [ObsoleteEx(ReplacementTypeOrMember = "MessageHandlerRegistry.Invoke(object handler, object potentialHandlerKind, object context)", RemoveInVersion = "7", TreatAsErrorFromVersion = "6")]
        public void InvokeTimeout(object handler, object state)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Clears the cache
        /// </summary>
        public void Clear()
        {
            handlerAndMessagesHandledByHandlerCache.Clear();
        }

        void CacheMethodForHandler(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), HandlerKind.Message, typeList);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), HandlerKind.Timeout, typeList);
            CacheMethod(handler, messageType, typeof(IHandleTimeout<>), HandlerKind.Timeout, typeList);
            CacheMethod(handler, messageType, typeof(IHandle<>), HandlerKind.Message, typeList);
            CacheMethod(handler, messageType, typeof(ISubscribe<>), HandlerKind.Event, typeList);
        }

        void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, HandlerKind potentialHandlerKind, ICollection<DelegateHolder> typeList)
        {
            Type contextType;
            var handleMethod = GetMethod(handler, messageType, interfaceGenericType, out contextType);
            if (handleMethod == null)
            {
                return;
            }
            Log.DebugFormat("Associated '{0}' message with '{1}' handler of kind {2}", messageType, handler, potentialHandlerKind);

            var delegateHolder = new DelegateHolder
            {
                MessageType = messageType.TypeHandle,
                ContextType = contextType.TypeHandle,
                HandlerKind = potentialHandlerKind,
                MethodDelegate = handleMethod
            };
            List<DelegateHolder> methodList;
            if (handlerAndMessagesHandledByHandlerCache.TryGetValue(handler.TypeHandle, out methodList))
            {
                methodList.Add(delegateHolder);
            }
            else
            {
                handlerAndMessagesHandledByHandlerCache[handler.TypeHandle] = new List<DelegateHolder>
                {
                    delegateHolder
                };
            }
        }

        static Action<object, object, object> GetMethod(Type targetType, Type messageType, Type interfaceGenericType, out Type contextType)
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
                        contextType = contextParameter.ParameterType;
                        var contextCastParam = Expression.Convert(contextParam, contextType);
                        var execute = Expression.Call(castTarget, methodInfo, messageCastParam, contextCastParam);
                        return Expression.Lambda<Action<object, object, object>>(execute, target, messageParam, contextParam).Compile();
                    }

                    contextType = typeof(NullContext);
                    var innerExecute = Expression.Call(castTarget, methodInfo, messageCastParam);
                    return Expression.Lambda<Action<object, object, object>>(innerExecute, target, messageParam, contextParam).Compile();
                }
            }

            contextType = null;
            return null;
        }

        static bool IsNewContextApi(ParameterInfo contextParameter)
        {
            return contextParameter != null;
        }

        static List<Type> GetMessageTypesBeingHandledBy(Type type)
        {
            return (from t in type.GetInterfaces()
                    where t.IsGenericType
                    let potentialMessageType = t.GetGenericArguments()[0]
                    where
                        typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandleTimeout<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(IHandle<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                        typeof(ISubscribe<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)

                    select potentialMessageType)
                   .Distinct()
                   .ToList();
        }

        class DelegateHolder
        {
            public RuntimeTypeHandle MessageType;
            public RuntimeTypeHandle ContextType;
            public HandlerKind HandlerKind;
            public Action<object, object, object> MethodDelegate;
        }

        class NullContext { }
    }
}