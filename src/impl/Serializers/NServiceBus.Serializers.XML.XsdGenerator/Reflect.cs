using System;
using System.Collections.Generic;
using NServiceBus.Utils.Reflection;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public static class Reflect
    {
        public static string GetTypeNameFrom(Type t)
        {
            if (t == typeof(int))
                return "xs:int";
            if (t == typeof(string))
                return "xs:string";
            if (t == typeof(double))
                return "xs:double";
            if (t == typeof(float))
                return "xs:float";
            if (t == typeof(bool))
                return "xs:boolean";
            if (t == typeof(Guid))
                return Strings.NamespacePrefix + ":guid";
            if (t == typeof(DateTime))
                return "xs:dateTime";
            if (t == typeof(TimeSpan))
                return "xs:duration";
            if (t == typeof(decimal))
                return "xs:decimal";
            if (t == typeof(DateTimeOffset))
                return "xs:dateTime";
            if (t == typeof(Object))
                return "xs:anyType";

            Type arrayType = GetEnumeratedTypeFrom(t);

            if (arrayType != null)
                return Strings.ArrayOf + GetTypeNameFrom(arrayType);

            return t.SerializationFriendlyName();
        }

        public static Type GetEnumeratedTypeFrom(Type t)
        {
            if (t.IsArray)
                return t.GetElementType();

            foreach (Type interfaceType in t.GetInterfaces())
            {
                Type[] genericArgs = interfaceType.GetGenericArguments();
                if (genericArgs.Length == 1)
                    if (typeof(IEnumerable<>).MakeGenericType(genericArgs[0]).IsAssignableFrom(t))
                        return genericArgs[0];
            }

            return null;
        }
    }
}
