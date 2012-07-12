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
        public static void Invoke(Type interfaceType,object handler, object message)
        {
            var messageTypesToMethods = handlerToMessageTypeToHandleMethodMap[handler.GetType()];
            foreach (var messageType in messageTypesToMethods.Keys)
                if (messageType.IsInstanceOfType(message))
                    messageTypesToMethods[messageType][interfaceType].Invoke(handler, new[] { message });
        }

        /// <summary>
        /// Registers the metod in the cache
        /// </summary>
        /// <param name="handler"></param>
        /// <param name="messageType"></param>
        public static void CacheMethodForHandler(Type handler, Type messageType)
        {
            if (!handlerToMessageTypeToHandleMethodMap.ContainsKey(handler))
                handlerToMessageTypeToHandleMethodMap.Add(handler, new Dictionary<Type, IDictionary<Type, MethodInfo>>());

            if (!(handlerToMessageTypeToHandleMethodMap[handler].ContainsKey(messageType)))
                handlerToMessageTypeToHandleMethodMap[handler].Add(messageType, GetHandleMethods(handler, messageType));
        }




        static IDictionary<Type,MethodInfo> GetHandleMethods(Type targetType, Type messageType)
        {
            var result = new Dictionary<Type, MethodInfo>();
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
                   result.Add(handlerInterface,method);
                
            }

            return result;
        }

        static readonly List<Type> handlerInterfaces = new List<Type> { typeof(IMessageHandler<>), typeof(IHandleTimeouts<>) };
        static readonly IDictionary<Type, IDictionary<Type, IDictionary<Type, MethodInfo>>> handlerToMessageTypeToHandleMethodMap = new Dictionary<Type, IDictionary<Type, IDictionary<Type, MethodInfo>>>();
    }
}
