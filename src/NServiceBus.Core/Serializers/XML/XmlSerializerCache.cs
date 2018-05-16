namespace NServiceBus
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Xml.Linq;
    using System.Xml.Serialization;
    using Logging;

    class XmlSerializerCache
    {
        public void InitType(Type t)
        {
            logger.Debug($"Initializing type: {t.AssemblyQualifiedName}");

            lock (lockObject)
            {
                InitTypeInternal(t);
            }
        }

        void InitTypeInternal(Type t)
        {
            if (typesBeingInitialized.Contains(t))
            {
                return;
            }

            typesBeingInitialized.Add(t);

            if (t.IsSimpleType())
            {
                return;
            }

            if (typeof(XContainer).IsAssignableFrom(t))
            {
                return;
            }

            if (typeof(IEnumerable).IsAssignableFrom(t))
            {
                if (t.IsArray)
                {
                    typesToCreateForArrays[t] = typeof(List<>).MakeGenericType(t.GetElementType());
                }

                foreach (var g in t.GetGenericArguments())
                {
                    InitTypeInternal(g);
                }

                //Handle dictionaries - initialize relevant KeyValuePair<T,K> types.
                foreach (var interfaceType in t.GetInterfaces())
                {
                    var arr = interfaceType.GetGenericArguments();
                    if (arr.Length != 1)
                    {
                        continue;
                    }

                    if (typeof(IEnumerable<>).MakeGenericType(arr[0]).IsAssignableFrom(t))
                    {
                        InitTypeInternal(arr[0]);
                    }
                }

                if (t.IsGenericType && t.IsInterface) //handle IEnumerable<Something>
                {
                    var g = t.GetGenericArguments();
                    var e = typeof(IEnumerable<>).MakeGenericType(g);

                    if (e.IsAssignableFrom(t))
                    {
                        typesToCreateForEnumerables[t] = typeof(List<>).MakeGenericType(g);
                    }
                }

                if (t.IsGenericType && t.GetGenericArguments().Length == 1)
                {
                    var setType = typeof(ISet<>).MakeGenericType(t.GetGenericArguments());

                    if (setType.IsAssignableFrom(t)) //handle ISet<Something>
                    {
                        var g = t.GetGenericArguments();
                        var e = typeof(IEnumerable<>).MakeGenericType(g);

                        if (e.IsAssignableFrom(t))
                        {
                            typesToCreateForEnumerables[t] = typeof(List<>).MakeGenericType(g);
                        }
                    }
                }

                return;
            }

            var isKeyValuePair = false;

            var args = t.GetGenericArguments();
            if (args.Length == 2)
            {
                isKeyValuePair = typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]) == t;
            }

            if (args.Length == 1 && args[0].IsValueType)
            {
                if (args[0].GetGenericArguments().Any() || typeof(Nullable<>).MakeGenericType(args[0]) == t)
                {
                    InitTypeInternal(args[0]);

                    if (!args[0].GetGenericArguments().Any())
                    {
                        return;
                    }
                }
            }

            if (typeMembers.ContainsKey(t))
            {
                return;
            }

            var props = GetAllPropertiesForType(t, isKeyValuePair);
            foreach (var p in props)
            {
                InitTypeInternal(p.PropertyType);
            }

            var fields = GetAllFieldsForType(t);
            foreach (var f in fields)
            {
                InitTypeInternal(f.FieldType);
            }

            // make the type only available once all properties & fields have been initialzed
            typeMembers[t] = new Tuple<FieldInfo[], PropertyInfo[]>(fields, props);
        }

        PropertyInfo[] GetAllPropertiesForType(Type t, bool isKeyValuePair)
        {
            var result = new List<PropertyInfo>();

            foreach (var prop in t.GetProperties())
            {
                if (!prop.CanWrite && !isKeyValuePair)
                {
                    continue;
                }

                if (prop.GetCustomAttribute<XmlIgnoreAttribute>(false) != null)
                {
                    continue;
                }

                if (typeof(IList) == prop.PropertyType)
                {
                    throw new NotSupportedException($"IList is not a supported property type for serialization, use List instead. Type: {t.FullName} Property: {prop.Name}");
                }

                var args = prop.PropertyType.GetGenericArguments();

                if (args.Length == 1)
                {
                    if (typeof(IList<>).MakeGenericType(args) == prop.PropertyType)
                    {
                        throw new NotSupportedException($"IList<T> is not a supported property type for serialization, use List<T> instead. Type: {t.FullName} Property: {prop.Name}");
                    }
                    if (typeof(ISet<>).MakeGenericType(args) == prop.PropertyType)
                    {
                        throw new NotSupportedException($"ISet<T> is not a supported property type for serialization, use HashSet<T> instead. Type: {t.FullName} Property: {prop.Name}");
                    }
                }

                if (args.Length == 2)
                {
                    if (typeof(IDictionary<,>).MakeGenericType(args[0], args[1]) == prop.PropertyType)
                    {
                        throw new NotSupportedException($"IDictionary<T, K> is not a supported property type for serialization, use Dictionary<T,K> instead. Type: {t.FullName} Property: {prop.Name}. Consider using a concrete Dictionary<T, K> instead, where T and K cannot be of type 'System.Object'");
                    }

                    if (args[0].FullName == "System.Object" || args[1].FullName == "System.Object")
                    {
                        throw new NotSupportedException($"Dictionary<T, K> is not a supported when Key or Value is of Type System.Object. Type: {t.FullName} Property: {prop.Name}. Consider using a concrete Dictionary<T, K> where T and K are not of type 'System.Object'");
                    }
                }

                result.Add(prop);
            }

            if (t.IsInterface)
            {
                foreach (var interfaceType in t.GetInterfaces())
                {
                    result.AddRange(GetAllPropertiesForType(interfaceType, false));
                }
            }

            return result.Distinct().ToArray();
        }

        FieldInfo[] GetAllFieldsForType(Type t)
        {
            return t.GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.Public);
        }

        readonly object lockObject = new object();

        ConcurrentBag<Type> typesBeingInitialized = new ConcurrentBag<Type>();

        public ConcurrentDictionary<Type, Type> typesToCreateForArrays = new ConcurrentDictionary<Type, Type>();
        public ConcurrentDictionary<Type, Type> typesToCreateForEnumerables = new ConcurrentDictionary<Type, Type>();

        public ConcurrentDictionary<Type, Tuple<FieldInfo[], PropertyInfo[]>> typeMembers = new ConcurrentDictionary<Type, Tuple<FieldInfo[], PropertyInfo[]>>();

        static ILog logger = LogManager.GetLogger<XmlSerializerCache>();
    }
}