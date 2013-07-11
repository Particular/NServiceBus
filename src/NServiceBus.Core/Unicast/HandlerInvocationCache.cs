namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using Saga;

    /// <summary>
    /// Helper that optimize the invocation of the handle methods
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
			Invoke(handler, message, HandlerCache);
        }

	    /// <summary>
		/// Invokes the timeout method of the given handler passing the message
		/// </summary>
		/// <param name="handler">The handler instance.</param>
		/// <param name="state">The message instance.</param>
        public static void InvokeTimeout(object handler, object state)
		{
			Invoke(handler, state, TimeoutCache);
		}

        /// <summary>
        /// Registers the method in the cache
        /// </summary>
        /// <param name="handler">The object type.</param>
        /// <param name="messageType">the message type.</param>
        public static void CacheMethodForHandler(Type handler, Type messageType)
        {
            CacheMethod(handler, messageType, typeof (IHandleMessages<>), HandlerCache);
            CacheMethod(handler, messageType, typeof (IHandleTimeouts<>), TimeoutCache);
        }

        /// <summary>
        /// Clears the cache
        /// </summary>
        public static void Clear()
        {
            HandlerCache.Clear();
            TimeoutCache.Clear();
        }

        static void Invoke(object handler, object message, Dictionary<RuntimeTypeHandle, List<DelegateHolder>> dictionary)
        {
            List<DelegateHolder> methodList;
            if (!dictionary.TryGetValue(handler.GetType().TypeHandle, out methodList))
            {
                return;
            }
            foreach (var delegateHolder in methodList.Where(x => x.MessageType.IsInstanceOfType(message)))
            {
                delegateHolder.MethodDelegate(handler, message);
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

		class DelegateHolder
		{
			public Type MessageType;
			public Action<object, object> MethodDelegate;
		}

		static readonly Dictionary<RuntimeTypeHandle, List<DelegateHolder>> HandlerCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();
		static readonly Dictionary<RuntimeTypeHandle, List<DelegateHolder>> TimeoutCache = new Dictionary<RuntimeTypeHandle, List<DelegateHolder>>();
    }
}
