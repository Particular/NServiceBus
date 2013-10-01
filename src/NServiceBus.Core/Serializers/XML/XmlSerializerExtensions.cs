namespace NServiceBus.Serializers.XML
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;

    static class XmlSerializerExtensions
    {

        public static bool IsSet(this Type type)
        {
            var genericArguments = type.GetGenericArguments();
            if (type.IsGenericType && genericArguments.Length == 1)
            {
                var setType = typeof(ISet<>).MakeGenericType(genericArguments);
                return setType.IsAssignableFrom(type);
            }
            return false;
        }

        public static bool IsGenericEnumerable(this Type type)
        {
            if (type.IsGenericType && type.IsInterface) //handle IEnumerable<Something>
            {
                var g = type.GetGenericArguments();
                var e = typeof(IEnumerable<>).MakeGenericType(g);

                return e.IsAssignableFrom(type);
            }
            return false;
        }

        public static bool IsList(this Type type)
        {
            var genericArguments = type.GetGenericArguments();
            if (type.IsGenericType && genericArguments.Length == 1) //handle IEnumerable<Something>
            {
                var g = genericArguments;
                var e = typeof(IList<>).MakeGenericType(g);
                return e.IsAssignableFrom(type);
            }
            return false;
        }

        public static bool IsIndexedProperty(this PropertyInfo propertyInfo)
        {
            return propertyInfo.GetIndexParameters().Length > 0;
        }
    }
}