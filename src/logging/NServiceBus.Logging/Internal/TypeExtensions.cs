using System;
using System.Linq.Expressions;
using System.Reflection;

namespace NServiceBus.Logging.Internal
{
    internal static class TypeExtensions
    {
        public static Func<object, T> GetInstancePropertyDelegate<T>(this Type type, string propertyName)
        {
            var propertyInfo = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (propertyInfo == null)
                throw new InvalidOperationException(String.Format("Could not find property {0} on type {1}", propertyName, type));

            var instanceParam = Expression.Parameter(typeof(object));
            var expr = Expression.Property(Expression.Convert(instanceParam, type), propertyInfo);

            return Expression.Lambda<Func<object, T>>(expr, new[] { instanceParam }).Compile();
        }

        public static Action<object, TParam1> GetInstanceMethodDelegate<TParam1>(this Type type, string methodName)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(TParam1) }, null);

            if (methodInfo == null)
                throw new InvalidOperationException(String.Format("Could not find method {0} on type {1}", methodName, type));
            
            var instanceParam = Expression.Parameter(typeof(object));
            var param1 = Expression.Parameter(typeof(TParam1));

            var expr = Expression.Call(Expression.Convert(instanceParam, type), methodInfo, new Expression[] { param1 });

            return Expression.Lambda<Action<object, TParam1>>(expr, new[] { instanceParam, param1 }).Compile();
        }

        public static Action<object, TParam1, TParam2> GetInstanceMethodDelegate<TParam1, TParam2>(this Type type, string methodName)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(TParam1), typeof(TParam2) }, null);

            if (methodInfo == null)
                throw new InvalidOperationException(String.Format("Could not find method {0} on type {1}", methodName, type));
            
            var instanceParam = Expression.Parameter(typeof(object));
            var param1 = Expression.Parameter(typeof(TParam1));
            var param2 = Expression.Parameter(typeof(TParam2));

            var expr = Expression.Call(Expression.Convert(instanceParam, type), methodInfo, new Expression[] { param1, param2 });

            return Expression.Lambda<Action<object, TParam1, TParam2>>(expr, new[] { instanceParam, param1, param2 }).Compile();
        }

        public static Func<T, TReturn> GetStaticFunctionDelegate<T, TReturn>(this Type type, string methodName)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null,
                                            new[] { typeof(T) }, null);

            if (methodInfo == null)
                throw new InvalidOperationException(String.Format("Could not find method {0} on type {1}", methodName, type));

            var param1 = Expression.Parameter(typeof(T));

            return Expression
              .Lambda<Func<T, TReturn>>(Expression.Call(methodInfo, param1), new[] { param1 })
              .Compile();
        }
    }
}