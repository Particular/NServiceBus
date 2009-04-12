using System;
using System.Collections;
using System.Collections.Generic;

namespace NServiceBus.Serializers.XML.XsdGenerator
{
    public static class Repository
    {
        /// <summary>
        /// Returns types in the order they were handled
        /// </summary>
        public static IEnumerable<ComplexType> ComplexTypes
        {
            get
            {
                List<Type> keys = new List<Type>(types.Keys);
                for (int i = keys.Count - 1; i >= 0; i-- )
                    yield return types[keys[i]];
            }
        }

        public static IEnumerable<Type> SimpleTypes
        {
            get { return simpleTypesToCreate; }
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
