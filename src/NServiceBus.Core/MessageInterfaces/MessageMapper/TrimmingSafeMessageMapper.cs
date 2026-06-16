#nullable enable

namespace NServiceBus.MessageInterfaces.MessageMapper.Reflection;

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

/// <summary>
/// A trimming/AOT-safe <see cref="IMessageMapper" /> that does not rely on dynamic code generation.
/// </summary>
/// <remarks>
/// Interface-based messages require a generated concrete proxy and therefore cannot be supported when
/// dynamic code generation is unavailable (trimming / NativeAOT). This mapper maps types as identity
/// (concrete types stay as-is) and instantiates concrete types directly. Any attempt to create or
/// resolve a proxy for an interface throws <see cref="NotSupportedException" /> at the point of use,
/// rather than at startup.
/// </remarks>
public sealed class TrimmingSafeMessageMapper : IMessageMapper
{
    /// <summary>
    /// Does nothing. Proxy generation is not supported without dynamic code.
    /// </summary>
    public void Initialize(IEnumerable<Type>? types)
    {
        // Intentionally a no-op. Interface mapping is not available without dynamic code generation,
        // and concrete types do not require initialization.
    }

    /// <summary>
    /// Returns the given type unchanged. No interface-to-concrete mapping is available without
    /// dynamic code generation.
    /// </summary>
    public Type? GetMappedTypeFor(Type t)
    {
        ArgumentNullException.ThrowIfNull(t);
        return t;
    }

    /// <summary>
    /// Returns <c>null</c>. No name-to-type mapping is available without dynamic code generation.
    /// </summary>
    public Type? GetMappedTypeFor(string typeName) => null;

    /// <summary>
    /// Instantiates a concrete message type. Instantiating an interface or abstract type throws
    /// <see cref="NotSupportedException" /> because proxy generation requires dynamic code.
    /// </summary>
    public T CreateInstance<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>() => (T)CreateInstance(typeof(T));

    /// <summary>
    /// Instantiates a concrete message type, then applies the given action. Instantiating an
    /// interface or abstract type throws <see cref="NotSupportedException" />.
    /// </summary>
    public T CreateInstance<[DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] T>(Action<T> action)
    {
        var result = CreateInstance<T>();
        action(result);
        return result;
    }

    /// <summary>
    /// Instantiates a concrete message type. Instantiating an interface or abstract type throws
    /// <see cref="NotSupportedException" /> because proxy generation requires dynamic code.
    /// </summary>
    public object CreateInstance([DynamicallyAccessedMembers(IMessageCreator.CreatorMembersRequired)] Type t)
    {
        ArgumentNullException.ThrowIfNull(t);

        if (t.IsInterface || t.IsAbstract)
        {
            throw new NotSupportedException(
                $"Creating an instance of interface or abstract type '{t.FullName}' is not supported when dynamic code generation is unavailable (e.g. under trimming or NativeAOT). " +
                "Use concrete message types instead of interface-based messages in these scenarios.");
        }

        return RuntimeHelpers.GetUninitializedObject(t);
    }
}