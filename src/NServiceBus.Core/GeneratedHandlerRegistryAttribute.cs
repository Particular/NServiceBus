#nullable enable
namespace NServiceBus;

using System;
using System.ComponentModel;

/// <summary>
/// Declares the generated entry type that registers all handlers and sagas of an assembly.
/// </summary>
/// <remarks>
/// The NServiceBus source generator emits this attribute for each assembly that has a generated handler registry.
/// The entry type is a public static class with <c>AddAll</c>, <c>AddAllHandlers</c>, and <c>AddAllSagas</c> methods
/// that take an <see cref="EndpointConfiguration" />. Calling <c>endpointConfiguration.Handlers.AddAll()</c> in an
/// endpoint project registers the handlers and sagas of every referenced assembly that declares the attribute.
/// </remarks>
/// <param name="entryType">The generated entry type.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
public sealed class GeneratedHandlerRegistryAttribute(Type entryType) : Attribute
{
    /// <summary>
    /// The generated entry type.
    /// </summary>
    public Type EntryType { get; } = entryType ?? throw new ArgumentNullException(nameof(entryType));
}
