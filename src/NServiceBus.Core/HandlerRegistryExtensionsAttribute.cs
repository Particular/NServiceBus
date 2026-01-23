#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a partial static class as the root for handler registry extensions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerRegistryExtensionsAttribute(string? entryPointName = null) : Attribute
{
    /// <summary>
    /// The name of the entry point which must be a valid C# identifier.
    /// </summary>
    public string? EntryPointName { get; } = entryPointName;
}