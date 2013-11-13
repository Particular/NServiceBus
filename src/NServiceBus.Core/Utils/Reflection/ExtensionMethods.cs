namespace NServiceBus.Utils.Reflection
{
    using System;
    using System.Collections;
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    /// <summary>
    /// Contains extension methods
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "5.0", Message= "These will be made internal in version 5.0")]
    public static class ExtensionMethods
    {
        /// <summary>
        /// Useful for finding if a type is (for example) IMessageHandler{T} where T : IMessage.
        /// </summary>  
        [ObsoleteEx(RemoveInVersion = "5.0", TreatAsErrorFromVersion = "4.3", Message= "No longer used. to be deleted")]
        public static bool IsGenericallyEquivalent(this Type type, Type openGenericType, Type genericArg)
        {
            var result = false;
            LoopAndAct(type, openGenericType, genericArg, t => result = true);

            return result;
        }

        /// <summary>
        /// Returns the enclosed generic type given that the type is GenericallyEquivalent.
        /// </summary>
        public static Type GetGenericallyContainedType(this Type type, Type openGenericType, Type genericArg)
        {
            Type result = null;
            LoopAndAct(type, openGenericType, genericArg, t => result = t);

            return result;
        }

        private static void LoopAndAct(Type type, Type openGenericType, Type genericArg, Action<Type> act)
        {
            foreach (var i in type.GetInterfaces())
            {
                var args = i.GetGenericArguments();

                if (args.Length == 1)
                    if (genericArg.IsAssignableFrom(args[0]))
                        if (openGenericType.MakeGenericType(args[0]) == i)
                        {
                            act(args[0]);
                            break;
                        }
            }
        }

        /// <summary>
        /// Returns true if the type can be serialized as is.
        /// </summary>
        public static bool IsSimpleType(this Type type)
        {
            return (type == typeof(string) ||
                    type.IsPrimitive ||
                    type == typeof(decimal) ||
                    type == typeof(Guid) ||
                    type == typeof(DateTime) ||
                    type == typeof(TimeSpan) ||
                    type == typeof(DateTimeOffset) ||
                    type.IsEnum);
        }

        /// <summary>
        /// Takes the name of the given type and makes it friendly for serialization
        /// by removing problematic characters.
        /// </summary>
        public static string SerializationFriendlyName(this Type t)
        {
            lock(TypeToNameLookup)
                if (TypeToNameLookup.ContainsKey(t))
                    return TypeToNameLookup[t];

            var index = t.Name.IndexOf('`');
            if (index >= 0)
            {
                var result = t.Name.Substring(0, index) + "Of";
                var args = t.GetGenericArguments();
                for (var i = 0; i < args.Length; i++)
                {
                    result += args[i].SerializationFriendlyName();
                    if (i != args.Length - 1)
                        result += "And";
                }

                if (args.Length == 2)
                    if (typeof(KeyValuePair<,>).MakeGenericType(args) == t)
                        result = "NServiceBus." + result;

                lock(TypeToNameLookup)  
                    TypeToNameLookup[t] = result;

                return result;
            }

            lock(TypeToNameLookup)
                TypeToNameLookup[t] = t.Name;

            return t.Name;
        }

        private static readonly byte[] MsPublicKeyToken = typeof(string).Assembly.GetName().GetPublicKeyToken();

        static bool IsClrType(byte[] a1)
        {
            IStructuralEquatable structuralEquatable = a1;
            return structuralEquatable.Equals(MsPublicKeyToken, StructuralComparisons.StructuralEqualityComparer);
        }

        private static readonly ConcurrentDictionary<Type, bool> IsSystemTypeCache = new ConcurrentDictionary<Type, bool>();

        public static bool IsSystemType(this Type type)
        {
            bool result;

            if (!IsSystemTypeCache.TryGetValue(type, out result))
            {
                var nameOfContainingAssembly = type.Assembly.GetName().GetPublicKeyToken();
                IsSystemTypeCache[type] = result = IsClrType(nameOfContainingAssembly);
            }

            return result;
        }

        public static bool IsNServiceBusMarkerInterface(this Type type)
        {
            return type == typeof(IMessage) ||
                   type == typeof(ICommand) ||
                   type == typeof(IEvent);
        }

        private static readonly IDictionary<Type, string> TypeToNameLookup = new Dictionary<Type, string>();
    }
}
