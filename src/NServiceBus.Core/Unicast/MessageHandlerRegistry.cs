namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
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
            Guard.AgainstNull(nameof(messageType), messageType);

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
        public void RegisterHandler(Type handlerType)
        {
            Guard.AgainstNull(nameof(handlerType), handlerType);

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
                    handlerAndMessagesHandledByHandlerCache[handlerType] = typeList = new List<DelegateHolder>();
                }

                CacheHandlerMethods(handlerType, messageType, typeList);
            }
        }

        /// <summary>
        /// Clears the cache.
        /// </summary>
        public void Clear()
        {
            handlerAndMessagesHandledByHandlerCache.Clear();
        }

        static void CacheHandlerMethods(Type handler, Type messageType, ICollection<DelegateHolder> typeList)
        {
            CacheMethod(handler, messageType, typeof(IHandleMessages<>), typeList, isTimeoutHandler: false);
            CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), typeList, isTimeoutHandler: true);
        }

        static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, ICollection<DelegateHolder> methodList, bool isTimeoutHandler)
        {
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

        static Func<object, object, IMessageHandlerContext, CancellationToken, Task> GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
        {
            var interfaceType = interfaceGenericType.MakeGenericType(messageType);

            if (!interfaceType.IsAssignableFrom(targetType))
            {
                return null;
            }

            var methodInfo = GetMethodForIHandleMessagesType(targetType, interfaceType, messageType);
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
            var cancellationTokenParam = Expression.Parameter(typeof(CancellationToken));

            if (methodParameters.Length == 3)
            {
                Expression body = Expression.Call(castTarget, methodInfo, messageCastParam, contextParam, cancellationTokenParam);
                return Expression.Lambda<Func<object, object, IMessageHandlerContext, CancellationToken, Task>>(body, target, messageParam, contextParam, cancellationTokenParam).Compile();
            }
            else
            {
                Expression body = Expression.Call(castTarget, methodInfo, messageCastParam, contextParam);
                return Expression.Lambda<Func<object, object, IMessageHandlerContext, CancellationToken, Task>>(body, target, messageParam, contextParam, cancellationTokenParam).Compile();
            }
        }

        static readonly string[] handlerMethodNames = new[] { "Handle", "HandleAsync" };
        static readonly string[] timeoutMethodNames = new[] { "Timeout", "TimeoutAsync" };

        static MethodInfo GetMethodForIHandleMessagesType(Type handlerType, Type interfaceType, Type messageType)
        {
            var methodNames = (interfaceType.GetGenericTypeDefinition() == typeof(IHandleTimeouts<>) ? timeoutMethodNames : handlerMethodNames);
            var eligibleMethods = handlerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy)
                .Where(methodInfo => CanBeHandleMethod(methodInfo, methodNames, messageType))
                .ToArray();

            if (eligibleMethods.Length == 0)
            {
                return null;
            }

            if (eligibleMethods.Length > 1)
            {
                // TODO: Better exception message
                throw new Exception("Too many Handle/HandleAsync methods for one message type.");
            }

            return eligibleMethods[0];
        }

        static bool CanBeHandleMethod(MethodInfo method, string[] methodNames, Type messageType)
        {
            if (method.ReturnType != typeof(Task) || !methodNames.Contains(method.Name))
            {
                return false;
            }

            var parameters = method.GetParameters();
            if (parameters.Length != 2 && parameters.Length != 3)
            {
                return false;
            }

            if (parameters[0].ParameterType != messageType || parameters[1].ParameterType != typeof(IMessageHandlerContext))
            {
                return false;
            }

            if (parameters.Length == 3 && parameters[2].ParameterType != typeof(CancellationToken))
            {
                return false;
            }

            return true;
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

        void ValidateHandlerType(Type handlerType)
        {
            var propertyTypes = handlerType.GetProperties().Select(p => p.PropertyType).ToList();
            var ctorArguments = handlerType.GetConstructors()
                .SelectMany(ctor => ctor.GetParameters().Select(p => p.ParameterType))
                .ToList();

            var dependencies = propertyTypes.Concat(ctorArguments).ToList();

            if (dependencies.Any(t => typeof(IMessageSession).IsAssignableFrom(t)))
            {
                throw new Exception($"Interfaces IMessageSession or IEndpointInstance should not be resolved from the container to enable sending or publishing messages from within sagas or message handlers. Instead, use the context parameter on the {handlerType.Name}.Handle method to send or publish messages.");
            }
        }

        readonly Dictionary<Type, List<DelegateHolder>> handlerAndMessagesHandledByHandlerCache = new Dictionary<Type, List<DelegateHolder>>();
        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

        class DelegateHolder
        {
            public bool IsTimeoutHandler { get; set; }
            public Type MessageType;
            public Func<object, object, IMessageHandlerContext, CancellationToken, Task> MethodDelegate;
        }
    }
}