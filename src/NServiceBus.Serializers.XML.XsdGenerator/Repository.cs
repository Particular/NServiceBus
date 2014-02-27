namespace NServiceBus.Serializers.XML.XsdGenerator
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public static class Repository
    {
        /// <summary>
        /// Returns types in the order they were handled
        /// </summary>
        public static IEnumerable<ComplexType> ComplexTypes
        {
            get
            {
                var keys = new List<Type>(types.Keys);
                for (var i = keys.Count - 1; i >= 0; i-- )
                    yield return types[keys[i]];
            }
        }

        public static IEnumerable<Type> SimpleTypes
        {
            get { return simpleTypesToCreate; }
        }

        public static void Handle(Type type)
        {
            var normalized = Normalize(type);
            if (normalized != type)
            {
                Handle(normalized);
                return;
            }

            if (types.ContainsKey(type))
                return;

            if (simpleTypesToCreate.Contains(type))
                return;

            var complex = ComplexType.Scan(type);
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
            foreach (var interfaceType in type.GetInterfaces())
            {
                var genericArgs = interfaceType.GetGenericArguments();
                if (genericArgs.Length != 1)
                    continue;

                if (typeof(IEnumerable<>).MakeGenericType(genericArgs[0]) == interfaceType)
                    return true;
            }

            return false;
        }

        private static Type Normalize(Type type)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type))
                return type;

            var enumerated = Reflect.GetEnumeratedTypeFrom(type);
            if (enumerated == null)
                return type;

            return typeof(List<>).MakeGenericType(enumerated);
        }

        private static readonly IDictionary<Type, ComplexType> types = new Dictionary<Type, ComplexType>();
        private static readonly IList<Type> simpleTypesToCreate = new List<Type>();
    }
}
