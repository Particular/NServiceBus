using System;
using System.Collections;
using System.Collections.Generic;

namespace NServiceBus.Serializers.InterfacesToXML.XsdGenerator
{
    public static class Repository
    {
        public static IEnumerable<ComplexType> ComplexTypes
        {
            get { return types.Values; }
        }

        public static void Handle(Type type)
        {
            Type normalized = Normalize(type);
            if (normalized != type)
            {
                Handle(normalized);
                return;
            }

            if (types.ContainsKey(type))
                return;

            if (simpleTypesToCreate.Contains(type))
                return;

            ComplexType complex = ComplexType.Scan(type);
            if (complex != null)
            {
                types[type] = complex;
                return;
            }

            if (type.IsEnum)
                if (!simpleTypesToCreate.Contains(type))
                    simpleTypesToCreate.Add(type);
        }

        public static bool IsNormalizedList(Type type)
        {
            Type[] genericArgs = type.GetGenericArguments();
            if (genericArgs == null)
                return false;

            if (genericArgs.Length != 1)
                return false;

            return typeof (List<>).MakeGenericType(genericArgs[0]) == type;
        }

        private static Type Normalize(Type type)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return type;

            Type enumerated = Reflect.GetEnumeratedTypeFrom(type);
            if (enumerated == null)
                return type;

            return typeof(List<>).MakeGenericType(enumerated);
        }

        private static readonly IDictionary<Type, ComplexType> types = new Dictionary<Type, ComplexType>();
        private static readonly IList<Type> simpleTypesToCreate = new List<Type>();
    }
}
