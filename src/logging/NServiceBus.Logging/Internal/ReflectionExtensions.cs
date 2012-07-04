using System;
using System.Linq;
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

        public static void SetProperty(this object instance, string propertyName, object val)
        {
            var type = instance.GetType();
            var pi = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            
            if (pi == null)
                throw new InvalidOperationException(String.Format("Could not find property {0} on type {1}", propertyName, type));

            pi.SetValue(instance, val, null);
        }

        public static object SetStaticProperty(this Type type, string propertyName, object val)
        {
            return type
                .InvokeMember(propertyName, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Static, null, null, new[] { val });
        }

        public static object GetStaticField(this Type type, string fieldName, bool ignoreCase = false)
        {
            var bindingFlags = BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static;
            if (ignoreCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            return type.InvokeMember(fieldName, bindingFlags, null, null, null);
        }

        public static object InvokeMethod(this object instance, string methodName, params object[] args)
        {
            return instance
                .GetType()
                .InvokeMember(methodName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, instance, args);
        }

        public static object InvokeStaticMethod(this Type type, string methodName, params object[] args)
        {
            var argTypes = args.Select(x => x.GetType()).ToArray();

            var methodInfo = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static, null, argTypes, null);

            if (methodInfo == null)
                throw new InvalidOperationException(String.Format("Could not find static method {0} on type {1}", methodName, type));

            return methodInfo.Invoke(null, args);
        }
    }
}