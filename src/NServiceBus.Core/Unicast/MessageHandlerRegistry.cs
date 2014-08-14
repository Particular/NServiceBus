namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Logging;
    using NServiceBus.Saga;

    /// <summary>
    ///     Maintains the message handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry : IMessageHandlerRegistry
    {
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
            if (!conventions.IsMessageType(messageType))
            {
                return Enumerable.Empty<Type>();
            }

            return from keyValue in handlerList
                where keyValue.Value.Any(msgTypeHandled => msgTypeHandled.IsAssignableFrom(messageType))
                select keyValue.Key;
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

            var messageTypesThisHandlerHandles = GetMessageTypesIfIsMessageHandler(handlerType).ToList();

            foreach (var messageType in messageTypesThisHandlerHandles)
            {
                List<Type> typeList;
                if (!handlerList.TryGetValue(handlerType, out typeList))
                {
                    handlerList[handlerType] = typeList = new List<Type>();
                }

                if (!typeList.Contains(messageType))
                {
                    typeList.Add(messageType);
                    Log.DebugFormat("Associated '{0}' message with '{1}' handler", messageType, handlerType);
                }

                HandlerInvocationCache.CacheMethodForHandler(handlerType, messageType);
            }
        }

        /// <summary>
        ///     If the type is a message handler, returns all the message types that it handles
        /// </summary>
        IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            return from t in type.GetInterfaces()
                where t.IsGenericType
                let potentialMessageType = t.GetGenericArguments()[0]
                where
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t) ||
                    typeof(IHandleTimeouts<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                select potentialMessageType;
        }

        static ILog Log = LogManager.GetLogger<MessageHandlerRegistry>();

        readonly Conventions conventions;
        readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();
    }
}