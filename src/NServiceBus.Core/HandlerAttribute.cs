#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a class as a handler.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)] // TODO Discuss
public sealed class HandlerAttribute : Attribute;