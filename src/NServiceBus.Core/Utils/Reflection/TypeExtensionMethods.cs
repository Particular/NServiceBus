namespace NServiceBus;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

static class TypeExtensionMethods
{
    extension(Type type)
    {
        /// <summary>
        /// Returns true if the type can be serialized as is.
        /// </summary>
        public bool IsSimpleType() =>
            type == typeof(string) ||
            type.IsPrimitive ||
            type == typeof(decimal) ||
            type == typeof(Guid) ||
            type == typeof(DateTime) ||
            type == typeof(TimeSpan) ||
            type == typeof(DateTimeOffset) ||
            type.IsEnum;

        public bool IsNullableType()
        {
            var args = type.GetGenericArguments();
            if (args.Length == 1 && args[0].IsValueType)
            {
                return type.GetGenericTypeDefinition() == typeof(Nullable<>);
            }

            return false;
        }

        public bool IsSystemType()
        {
            if (IsSystemTypeCache.TryGetValue(type.TypeHandle, out var result))
            {
                return result;
            }

            var publicKeyTokenOfContainingAssembly = type.Assembly.GetName().GetPublicKeyToken();
            IsSystemTypeCache[type.TypeHandle] = result = IsClrType(publicKeyTokenOfContainingAssembly);
            return result;
        }

        public bool IsFromParticularAssembly() => type.Assembly.IsParticularAssembly();
    }

    extension([NotNull] Type t)
    {
        /// <summary>
        /// Takes the name of the given type and makes it friendly for serialization
        /// by removing problematic characters.
        /// </summary>
        public string SerializationFriendlyName() =>
            TypeToNameLookup.GetOrAdd(t.TypeHandle, static (typeHandle, t) =>
            {
                var index = t.Name.IndexOf('`');
                if (index >= 0)
                {
                    var result = string.Concat(t.Name.AsSpan(0, index), "Of");
                    var args = t.GetGenericArguments();
                    for (var i = 0; i < args.Length; i++)
                    {
                        result += args[i].SerializationFriendlyName();
                        if (i != args.Length - 1)
                        {
                            result += "And";
                        }
                    }

                    if (args.Length == 2)
                    {
                        if (typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]) == t)
                        {
                            result = "NServiceBus." + result;
                        }
                    }

                    return result;
                }

                return Type.GetTypeFromHandle(typeHandle)?.Name;
            }, t);
    }

    static bool IsClrType(ReadOnlySpan<byte> publicKeyToken) => publicKeyToken.SequenceEqual(MsPublicKeyToken);

    static readonly byte[] MsPublicKeyToken = typeof(string).Assembly.GetName().GetPublicKeyToken();

    static readonly ConcurrentDictionary<RuntimeTypeHandle, bool> IsSystemTypeCache = new();
    static readonly ConcurrentDictionary<RuntimeTypeHandle, string> TypeToNameLookup = new();
}