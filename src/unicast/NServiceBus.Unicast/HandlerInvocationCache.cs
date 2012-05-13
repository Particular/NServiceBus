namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using Saga;

    /// <summary>
    /// Helper that optimize the invokation of the handle methods
    /// </summary>
    public class HandlerInvocationCache
    {
        /// <summary>
        /// Invokes the handle method of the given handler passing the message
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="message"></param>
        public static void Invoke(object handler, object message)
        {
            var messageTypesToMethods = handlerToMessageTypeToHandleMethodMap[handler.GetType()];
            foreach (var messageType in messageTypesToMethods.Keys)
                if (messageType.IsInstanceOfType(message))
                    messageTypesToMethods[messageType].Invoke(handler, new[] { message });
        }

        /// <summary>
        /// Registers the metod in the cache
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="messageType"></param>
        public static void CacheMethodForHandler(Type handler, Type messageType)
        {
            CacheMethodForHandler(handler,messageType,GetHandleMethod(handler, messageType));
        }


        /// <summary>
        /// Registers the metod in the cache
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="messageType"></param>
        public static void CacheMethodForHandler(Type handler, Type messageType,MethodInfo method)
        {
            if (!handlerToMessageTypeToHandleMethodMap.ContainsKey(handler))
                handlerToMessageTypeToHandleMethodMap.Add(handler, new Dictionary<Type, MethodInfo>());

            if (!(handlerToMessageTypeToHandleMethodMap[handler].ContainsKey(messageType)))
                handlerToMessageTypeToHandleMethodMap[handler].Add(messageType, method);
        }



        static MethodInfo GetHandleMethod(Type targetType, Type messageType)
        { 
            foreach (var handlerInterface in handlerInterfaces)
            {
                MethodInfo method = null;
                try
                {
                    method = targetType.GetInterfaceMap(handlerInterface.MakeGenericType(messageType))
                        .TargetMethods
                        .FirstOrDefault();

                }
                catch
                {
                    //intentionally swallow
                }

                if (method != null)
                    return method;
                
            }

            return null;
        }

        static readonly List<Type> handlerInterfaces = new List<Type> { typeof(IMessageHandler<>), typeof(IHandleTimeouts<>) }; 

        static readonly IDictionary<Type, IDictionary<Type, MethodInfo>> handlerToMessageTypeToHandleMethodMap = new Dictionary<Type, IDictionary<Type, MethodInfo>>();
    }
}