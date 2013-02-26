using System.Linq.Expressions;

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
			Invoke(handler, state, timeoutCahce);
		}

		static void Invoke(object handler, object message, Dictionary<RuntimeTypeHandle, List<DelegatetHolder>> dictionary)
		{
			List<DelegatetHolder> methodList;
			if (!dictionary.TryGetValue(handler.GetType().TypeHandle, out methodList))
			{
				return;
			}
			foreach (var delegatetHolder in methodList.Where(x => x.MessageType.IsInstanceOfType(message)))
			{
				delegatetHolder.MethodDelegate(handler, message);
			}
		}

        /// <summary>
        /// Registers the method in the cache
        /// </summary>
        /// <param name="handler">The object type.</param>
        /// <param name="messageType">the message type.</param>
        public static void CacheMethodForHandler(Type handler, Type messageType)
		{
	        CacheMethod(handler, messageType, typeof (IMessageHandler<>), handlerCache);
			CacheMethod(handler, messageType, typeof(IHandleTimeouts<>), timeoutCahce);
        }

	    static void CacheMethod(Type handler, Type messageType, Type interfaceGenericType, Dictionary<RuntimeTypeHandle, List<DelegatetHolder>> cache)
	    {
		    var handleMethod = GetMethod(handler, messageType, interfaceGenericType);
		    if (handleMethod == null)
		    {
			    return;
		    }
		    var delegatetHolder = new DelegatetHolder
			    {
				    MessageType = messageType,
				    MethodDelegate = handleMethod
			    };
		    List<DelegatetHolder> methodList;
		    if (cache.TryGetValue(handler.TypeHandle, out methodList))
		    {
			    if (methodList.Any(x => x.MessageType == messageType))
			    {
				    return;
			    }
			    methodList.Add(delegatetHolder);
		    }
		    else
		    {
			    cache[handler.TypeHandle] = new List<DelegatetHolder>
				    {
					    delegatetHolder
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
				    var target = Expression.Parameter(typeof (object));
				    var param = Expression.Parameter(typeof (object));

				    var castTarget = Expression.Convert(target, targetType);
				    var castParam = Expression.Convert(param, methodInfo.GetParameters().First().ParameterType);
				    var execute = Expression.Call(castTarget, methodInfo, castParam);
				    return Expression.Lambda<Action<object, object>>(execute, target, param).Compile();
			    }
		    }
		    return null;
	    }
		class DelegatetHolder
		{
			public Type MessageType;
			public Action<object, object> MethodDelegate;
		}
		static Dictionary<RuntimeTypeHandle, List<DelegatetHolder>> handlerCache = new Dictionary<RuntimeTypeHandle, List<DelegatetHolder>>();
		static Dictionary<RuntimeTypeHandle, List<DelegatetHolder>> timeoutCahce = new Dictionary<RuntimeTypeHandle, List<DelegatetHolder>>();
    }

	public class HandlerInvocationCacheOld
	{
		/// <summary>
		/// Invokes the handle method of the given handler passing the message
		/// </summary>
		/// <param name="interfaceType">The method that implements the interface type to execute.</param>
		/// <param name="handler">The handler instance.</param>
		/// <param name="message">The message instance.</param>
		public static void Invoke(Type interfaceType, object handler, object message)
		{
			var messageTypesToMethods = handlerToMessageTypeToHandleMethodMap[handler.GetType()];
			foreach (var messageType in messageTypesToMethods.Keys)
				if (messageType.IsInstanceOfType(message))
					messageTypesToMethods[messageType][interfaceType].Invoke(handler, new[] { message });
		}

		/// <summary>
		/// Registers the method in the cache
		/// </summary>
		/// <param name="handler">The object type.</param>
		/// <param name="messageType">the message type.</param>
		public static void CacheMethodForHandler(Type handler, Type messageType)
		{
			if (!handlerToMessageTypeToHandleMethodMap.ContainsKey(handler))
				handlerToMessageTypeToHandleMethodMap.Add(handler, new Dictionary<Type, IDictionary<Type, MethodInfo>>());

			if (!(handlerToMessageTypeToHandleMethodMap[handler].ContainsKey(messageType)))
				handlerToMessageTypeToHandleMethodMap[handler].Add(messageType, GetHandleMethods(handler, messageType));
		}

		static IDictionary<Type, MethodInfo> GetHandleMethods(Type targetType, Type messageType)
		{
			var result = new Dictionary<Type, MethodInfo>();

			foreach (var handlerInterface in handlerInterfaces)
			{
				MethodInfo method = null;

				var interfaceType = handlerInterface.MakeGenericType(messageType);

				if (interfaceType.IsAssignableFrom(targetType))
				{
					method = targetType.GetInterfaceMap(interfaceType)
						.TargetMethods
						.FirstOrDefault();
				}

				if (method != null)
				{
					result.Add(handlerInterface, method);
				}
			}

			return result;
		}

		static readonly List<Type> handlerInterfaces = new List<Type> { typeof(IMessageHandler<>), typeof(IHandleTimeouts<>) };
		static readonly IDictionary<Type, IDictionary<Type, IDictionary<Type, MethodInfo>>> handlerToMessageTypeToHandleMethodMap = new Dictionary<Type, IDictionary<Type, IDictionary<Type, MethodInfo>>>();
	}
}
