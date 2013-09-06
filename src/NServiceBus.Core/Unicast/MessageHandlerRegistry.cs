namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Logging;

    /// <summary>
    /// Maintains the message handlers for this endpoint
    /// </summary>
    public class MessageHandlerRegistry : IMessageHandlerRegistry
    {
        /// <summary>
        /// Registers the given message handler type
        /// </summary>
        /// <param name="handlerType"></param>
        public void RegisterHandler(Type handlerType)
        {
            if (handlerType.IsAbstract)
                return;

            var messageTypesThisHandlerHandles = GetMessageTypesIfIsMessageHandler(handlerType).ToList();


            foreach (var messageType in messageTypesThisHandlerHandles)
            {
                if (!handlerList.ContainsKey(handlerType))
                    handlerList.Add(handlerType, new List<Type>());

                if (!(handlerList[handlerType].Contains(messageType)))
                {
                    handlerList[handlerType].Add(messageType);
                    Log.DebugFormat("Associated '{0}' message with '{1}' handler", messageType, handlerType);
                }

                HandlerInvocationCache.CacheMethodForHandler(handlerType, messageType);
            }
        }

        /// <summary>
        /// Gets the list of <see cref="IMessageHandler{T}"/> <see cref="Type"/>s for the given <paramref name="messageType"/>
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        public IEnumerable<Type> GetHandlerTypes(Type messageType)
        {
            foreach (var handlerType in handlerList.Keys)
                foreach (var msgTypeHandled in handlerList[handlerType])
                    if (msgTypeHandled.IsAssignableFrom(messageType))
                    {
                        yield return handlerType;
                        break;
                    }
        }

        /// <summary>
        /// Lists all message type for which we have handlers
        /// </summary>
        /// <returns></returns>
        public IEnumerable<Type> GetMessageTypes()
        {
            foreach (var handlerType in handlerList.Keys)
                foreach (var typeHandled in handlerList[handlerType])
                    if (MessageConventionExtensions.IsMessageType(typeHandled))
                        yield return typeHandled;
            
        }


        /// <summary>
        /// If the type is a message handler, returns all the message types that it handles
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static IEnumerable<Type> GetMessageTypesIfIsMessageHandler(Type type)
        {
            foreach (var t in type.GetInterfaces().Where(t => t.IsGenericType))
            {
              
                var potentialMessageType = t.GetGenericArguments().SingleOrDefault();

                if (potentialMessageType == null)
                    continue;

             
                if (MessageConventionExtensions.IsMessageType(potentialMessageType) ||
                    typeof(IHandleMessages<>).MakeGenericType(potentialMessageType).IsAssignableFrom(t))
                    yield return potentialMessageType;
            }
        }



        readonly IDictionary<Type, List<Type>> handlerList = new Dictionary<Type, List<Type>>();

        private readonly static ILog Log = LogManager.GetLogger(typeof(MessageHandlerRegistry));

    }
}