namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection
{
    using System;
    using System.Collections.Generic;
    using Utils.Reflection;

    static class TypeNameExtensions
    {

        public static string GetTypeName(this Type t)
        {
            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                if (typeof(KeyValuePair<,>).MakeGenericType(args) == t)
                {
                    return t.SerializationFriendlyName();
                }
            }
            return t.FullName;
        }
    }
}