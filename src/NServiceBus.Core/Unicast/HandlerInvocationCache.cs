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
        /// <param name="handler">The handler instance.</param>
        /// <param name="message">The message instance.</param>
        public static void InvokeHandle(object handler, object message)
        {
			Invoke(handler, message, handlerCache);
        }

	    /// <summary>
		/// Invokes the timout method of the given handler passing the message
		/// </summary>
		/// <param name="handler">The handler instance.</param>
		/// <param name="state">The message instance.</param>
        public static void InvokeTimeout(object handler, object state)
		{
			Invoke(handler, state, timoutCahce);
		}

		static void Invoke(object handler, object message, Dictionary<Type, Dictionary<Type, MethodInfo>> dictionary)
		{
			Dictionary<Type, MethodInfo> methodList;
			if (!dictionary.TryGetValue(handler.GetType(), out methodList))
			{
				return;
			}
			foreach (var pair in methodList.Where(pair => pair.Key.IsInstanceOfType(message)))
			{
				pair.Value.Invoke(handler, new[] { message });
			}
		}

        /// <summary>
        /// Registers the method in the cache
        /// </summary>
        /// <param name="handler">The object type.</param>
        /// <param name="messageType">the message type.</param>
        public static void CacheMethodForHandler(Type handler, Type messageType)
        {
	        var handleMethod = GetMethod(handler, messageType, typeof(IMessageHandler<>));
			if (handleMethod != null)
			{
				Dictionary<Type, MethodInfo> methodList;
				if (!handlerCache.TryGetValue(handler, out methodList))
				{
					handlerCache[handler] = methodList = new Dictionary<Type, MethodInfo>();
				}
				methodList[messageType] = handleMethod;
			}

	        var timoutMethod = GetMethod(handler, messageType,  typeof (IHandleTimeouts<>));
			if (timoutMethod != null)
			{
				Dictionary<Type, MethodInfo> methodList;
				if (!timoutCahce.TryGetValue(handler, out methodList))
				{
					timoutCahce[handler] = methodList = new Dictionary<Type, MethodInfo>();
				}
				methodList[messageType] = timoutMethod;
			}
        }

	    static MethodInfo GetMethod(Type targetType, Type messageType, Type interfaceGenericType)
	    {
		    var interfaceType = interfaceGenericType.MakeGenericType(messageType);

		    if (interfaceType.IsAssignableFrom(targetType))
		    {
			    return targetType.GetInterfaceMap(interfaceType)
			                     .TargetMethods
			                     .FirstOrDefault();
		    }
		    return null;
	    }

	    static Dictionary<Type, Dictionary<Type, MethodInfo>> handlerCache = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
		static Dictionary<Type, Dictionary<Type, MethodInfo>> timoutCahce = new Dictionary<Type, Dictionary<Type, MethodInfo>>();
    }
}
