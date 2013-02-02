namespace NServiceBus.Logging.Internal
{
    using System;
    using System.Linq;
    using System.Reflection;

    internal static class ReflectionExtensions
    {

        public static object SetStaticProperty(this Type type, string propertyName, object val)
        {
            return type
                .InvokeMember(propertyName, BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.Static, null, null, new[] { val });
        }

        public static object GetStaticProperty(this Type type, string propertyName)
        {
            return type
                .InvokeMember(propertyName, BindingFlags.GetProperty | BindingFlags.Public | BindingFlags.Static, null, null, null);
        }

        public static object GetStaticField(this Type type, string fieldName, bool ignoreCase = false)
        {
            var bindingFlags = BindingFlags.GetField | BindingFlags.Public | BindingFlags.Static;
            if (ignoreCase)
                bindingFlags |= BindingFlags.IgnoreCase;

            return type.InvokeMember(fieldName, bindingFlags, null, null, null);
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