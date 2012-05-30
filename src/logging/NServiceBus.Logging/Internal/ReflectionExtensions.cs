using System;
using System.Reflection;

namespace NServiceBus.Logging.Internal
{
    internal static class ReflectionExtensions
    {
        public static object GetProperty(this object instance, string propertyName)
        {
            return instance
                .GetType()
                .InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Instance, null, instance, null);
        }

        public static object SetProperty(this object instance, string propertyName, object val)
        {
            return instance
                .GetType()
                .InvokeMember(propertyName, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Instance, null, instance, new[] { val });
        }

        public static object GetStaticField(this Type type, string fieldName)
        {
            return type.InvokeMember(fieldName, BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static, null, null, null);
        }

        public static object InvokeMethod(this object instance, string methodName, params object[] args)
        {
            return instance
                .GetType()
                .InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, instance, args);
        }

        public static object InvokeStaticMethod(this Type type, string methodName, Type[] argTypes, object[] args)
        {
            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, argTypes, null);

            if (methodInfo == null)
                throw new InvalidOperationException(String.Format("Could not find static method {0} on type {1}", methodName, type));

            return methodInfo.Invoke(null, args);
        }
    }
}