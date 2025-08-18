#nullable enable
namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection;

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;

/// <summary>
/// Uses reflection to map between interfaces and their generated concrete implementations.
/// </summary>
public class MessageMapper : IMessageMapper
{
    /// <summary>
    /// Initializes a new instance of <see cref="MessageMapper" />.
    /// </summary>
    public MessageMapper() => concreteProxyCreator = new ConcreteProxyCreator();

    /// <summary>
    /// Scans the given types generating concrete classes for interfaces.
    /// </summary>
    public void Initialize(IEnumerable<Type>? types)
    {
        if (types == null)
        {
            return;
        }

        foreach (var t in types)
        {
            InitType(t);
        }
    }

    /// <summary>
    /// If the given type is concrete, returns the interface it was generated to support.
    /// If the given type is an interface, returns the concrete class generated to implement it.
    /// </summary>
    public Type? GetMappedTypeFor(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        RuntimeTypeHandle typeHandle;

        if (t.IsInterface)
        {
            return interfaceToConcreteTypeMapping.TryGetValue(t.TypeHandle, out typeHandle) ? Type.GetTypeFromHandle(typeHandle) : null;
        }

        if (t.IsGenericTypeDefinition)
        {
            return null;
        }

        return concreteToInterfaceTypeMapping.TryGetValue(t.TypeHandle, out typeHandle) ? Type.GetTypeFromHandle(typeHandle) : t;
    }

    /// <summary>
    /// Returns the type mapped to the given name.
    /// </summary>
    public Type? GetMappedTypeFor(string typeName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(typeName);
        var name = typeName;
        if (typeName.EndsWith(ConcreteProxyCreator.SUFFIX, StringComparison.Ordinal))
        {
            name = typeName[..^ConcreteProxyCreator.SUFFIX.Length];
        }

        return nameToType.TryGetValue(name, out var typeHandle) ? Type.GetTypeFromHandle(typeHandle) : null;
    }

    /// <summary>
    /// Calls the <see cref="CreateInstance(Type)" /> and returns its result cast to <typeparamref name="T" />.
    /// </summary>
    public T CreateInstance<T>() => (T)CreateInstance(typeof(T));

    /// <summary>
    /// Calls the generic CreateInstance and performs the given action on the result.
    /// </summary>
    public T CreateInstance<T>(Action<T> action)
    {
        var result = (T)CreateInstance(typeof(T));

        action(result);

        return result;
    }

    /// <summary>
    /// If the given type is an interface, finds its generated concrete implementation, instantiates it, and returns the
    /// result.
    /// </summary>
    public object CreateInstance(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        InitType(t);

        if ((t.IsInterface || t.IsAbstract) && GetMappedTypeFor(t) is Type mapped)
        {
            return RuntimeHelpers.GetUninitializedObject(mapped);
        }

        if (typeToConstructor.TryGetValue(t.TypeHandle, out var ctor) && MethodBase.GetMethodFromHandle(ctor, t.TypeHandle) is ConstructorInfo constructorInfo && constructorInfo.Invoke(null) is { } instance)
        {
            return instance;
        }

        return RuntimeHelpers.GetUninitializedObject(t);
    }

    void InitType(Type? t)
    {
        if (t == null || initializedTypes.ContainsKey(t))
        {
            return;
        }

        InnerInitialize(t);

        _ = initializedTypes.TryAdd(t, true);
    }

    void InnerInitialize(Type t)
    {
        if (t.IsSimpleType() || t.IsGenericTypeDefinition)
        {
            return;
        }

        if (typeof(IEnumerable).IsAssignableFrom(t))
        {
            InitType(t.GetElementType());

            foreach (var interfaceType in t.GetInterfaces())
            {
                foreach (var g in interfaceType.GetGenericArguments())
                {
                    if (g == t)
                    {
                        continue;
                    }

                    InitType(g);
                }
            }

            return;
        }

        var typeName = GetTypeName(t);

        // check and proxy generation is not threadsafe
        lock (messageInitializationLock)
        {
            //already handled this type, prevent infinite recursion
            if (nameToType.ContainsKey(typeName))
            {
                return;
            }

            if (t.IsInterface)
            {
                GenerateImplementationFor(t);
            }
            else
            {
                var constructorInfo = t.GetConstructor(Type.EmptyTypes);
                if (constructorInfo != null)
                {
                    typeToConstructor[t.TypeHandle] = constructorInfo.MethodHandle;
                }
            }

            nameToType[typeName] = t.TypeHandle;
        }

        if (!t.IsInterface)
        {
            return;
        }

        foreach (var field in t.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
        {
            InitType(field.FieldType);
        }

        foreach (var prop in t.GetProperties())
        {
            InitType(prop.PropertyType);
        }
    }

    void GenerateImplementationFor(Type interfaceType)
    {
        if (!interfaceType.IsVisible)
        {
            throw new Exception($"Cannot generate a concrete implementation for '{interfaceType}' because it is not public. Ensure that all interfaces used as messages are public.");
        }

        if (interfaceType.GetMethods().Any(mi => !(mi.IsSpecialName && (mi.Name.StartsWith("set_") || mi.Name.StartsWith("get_")))))
        {
            throw new Exception($"Cannot generate a concrete implementation for '{interfaceType}' because it contains methods. Ensure that all interfaces used as messages do not contain methods.");
        }
        var mapped = concreteProxyCreator.CreateTypeFrom(interfaceType);
        interfaceToConcreteTypeMapping[interfaceType.TypeHandle] = mapped.TypeHandle;
        concreteToInterfaceTypeMapping[mapped.TypeHandle] = interfaceType.TypeHandle;
        var constructorInfo = mapped.GetConstructor(Type.EmptyTypes);
        if (constructorInfo != null)
        {
            typeToConstructor[mapped.TypeHandle] = constructorInfo.MethodHandle;
        }
    }

    static string GetTypeName(Type t)
    {
        var args = t.GetGenericArguments();
        if (args.Length == 2)
        {
            if (typeof(KeyValuePair<,>).MakeGenericType(args[0], args[1]) == t)
            {
                return t.SerializationFriendlyName();
            }
        }
        return t.FullName!;
    }

    readonly Lock messageInitializationLock = new();

    readonly ConcurrentDictionary<Type, bool> initializedTypes = new();

    readonly ConcreteProxyCreator concreteProxyCreator;
    readonly ConcurrentDictionary<RuntimeTypeHandle, RuntimeTypeHandle> concreteToInterfaceTypeMapping = new();
    readonly ConcurrentDictionary<RuntimeTypeHandle, RuntimeTypeHandle> interfaceToConcreteTypeMapping = new();
    readonly ConcurrentDictionary<string, RuntimeTypeHandle> nameToType = new();
    readonly ConcurrentDictionary<RuntimeTypeHandle, RuntimeMethodHandle> typeToConstructor = new();
}