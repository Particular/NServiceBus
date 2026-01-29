#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a partial static class as the root for handler registry extensions.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerRegistryExtensionsAttribute : Attribute
{
    /// <summary>
    /// The name of the entry point which must be a valid C# identifier.
    /// </summary>
    public string? EntryPointName { get; init; }

    /// <summary>
    /// Optional regex replacement patterns in the form "pattern=>replacement" applied to the handler or saga type name to customize generated registration method names.
    /// </summary>
    /// <remarks>
    /// Examples (applied to the handler or saga type name):
    /// <code>
    /// Handler$=&gt;Register
    /// OrderShippedHandler -&gt; OrderShippedRegister
    ///
    /// ^(.+)$=&gt;Register$1
    /// OrderShippedHandler -&gt; RegisterOrderShippedHandler
    ///
    /// ^(.*)Handler$=&gt;Add$1
    /// OrderShippedHandler -&gt; AddOrderShipped
    ///
    /// ^(.*)Saga$=&gt;Register$1Saga
    /// OrderShippingSaga -&gt; RegisterOrderShippingSaga
    ///
    /// ^(.*)Policy$=&gt;Add$1Policy
    /// OrderShippingPolicy -&gt; AddOrderShippingPolicy
    /// </code>
    /// The patterns are applied in the order they are specified, and the first matching pattern is used. If no patterns match, a default naming convention is used.
    /// The more complex the patterns are the more time the source generator will take to execute.
    /// </remarks>
    public string[]? RegistrationMethodNamePatterns { get; init; }
}
