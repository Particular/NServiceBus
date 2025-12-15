#nullable enable
namespace NServiceBus;

using System;

/// <summary>
///
/// </summary>
/// <param name="category"></param>
[AttributeUsage(AttributeTargets.Class)]
public sealed class HandlerAttribute(string category) : Attribute
{
    /// <summary>
    ///
    /// </summary>
    public string Category { get; } = category;
}