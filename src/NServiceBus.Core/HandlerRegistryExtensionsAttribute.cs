#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a partial static class as the root for handler registry extensions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerRegistryExtensionsAttribute : Attribute;
