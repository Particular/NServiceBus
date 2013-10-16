namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    ///     Maintains the message handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry : IMessageHandlerRegistry
    {
        /// <summary>
        ///     Gets the list of <see cref="IMessageHandler{T}" /> <see cref="Type" />s for the given
        ///     <paramref name="messageType" />
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            return from keyValue in handlerList
                   where keyValue.Value.Any(msgTypeHandled => msgTypeHandled.IsAssignableFrom(messageType))
                   select keyValue.Key;
        }

        /// <summary>
        ///     Lists all message type for which we have handlers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> GetMessageTypes()
        {
            return from handlers in handlerList.Values
                   from typeHandled in handlers
                   where MessageConventionExtensions.IsMessageType(typeHandled)
                   select typeHandled;
        }

        /// <summary>
        ///     Registers the given message handler type
        /// </summary>
        /// <param name="handlerType"></param>
        public void RegisterHandler(Type handlerType)
        {
            if (handlerType.IsAbstract)
            {
                return;
            }

            var messageTypesThisHandlerHandles = GetMessageTypesIfIsMessageHandler(handlerType).ToList();


            foreach (var messageType in messageTypesThisHandlerHandles)
            {
                if (!handlerList.ContainsKey(handlerType))
                {
                    handlerList.Add(handlerType, new List<Type>());
                }

                if (!(handlerList[handlerType].Contains(messageType)))
                {
                    handlerList[handlerType].Add(messageType);
                    Log.DebugFormat("Associated '{0}' message with '{1}' handler", messageType, handlerType);
                }

                HandlerInvocationCache.CacheMethodForHandler(handlerType, messageType);
            }
        }

        /// <summary>
        ///     If the type is a message handler, returns all the message types that it handles
        /// </summary>
        static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            return from t in type.GetInterfaces()
                where t.IsGenericType
                let potentialMessageType = t.GetGenericArguments()[0]
                where
                    MessageConventionExtensions.IsMessageType(potentialMessageType) ||
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t)
                select potentialMessageType;
        }

        static readonly ILog Log = LogManager.GetLogger(typeof(MessageHandlerRegistry));
        readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();
    }
}