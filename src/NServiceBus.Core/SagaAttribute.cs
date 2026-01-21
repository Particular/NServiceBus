#nullable enable

namespace NServiceBus;

using System;

/// <summary>
/// Marks a class as a saga.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)] // TODO Discuss
public sealed class SagaAttribute : Attribute;