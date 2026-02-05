#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a partial static class as the root for handler registry extensions to enable customization of the source-generated handler registration API.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="HandlerRegistryExtensionsAttribute" /> is optional and provides advanced customization options for the source-generated handler registration API.
/// When not applied, the source generator uses default conventions based on the assembly name.
/// </para>
///
/// <para>
/// This attribute controls the visibility and naming of the generated extension methods:
/// <list type="bullet">
/// <item><description>The entry point name (defaults to the assembly name when not specified).</description></item>
/// <item><description>The namespace where the generated methods are placed.</description></item>
/// <item><description>The naming pattern for individual handler registration methods.</description></item>
/// </list>
/// </para>
///
/// <para>
/// When applied to a partial static class, the source generator creates extension methods on <see cref="HandlerRegistry"/>
/// accessible via <see cref="EndpointConfiguration">endpointConfiguration</see>.<see cref="HandlerRegistry">Handlers</see>.
/// For example, with the default configuration: <c>endpointConfiguration.Handlers.MyAssemblyName.AddShipOrderHandler()</c>.
/// </para>
///
/// <para>
/// Use the <see cref="EntryPointName" /> property to override the default assembly-based entry point name.
/// The entry point name must be a valid C# identifier.
/// Use the <see cref="RegistrationMethodNamePatterns" /> property to customize how handler registration method names are generated
/// from handler type names using regex replacement patterns.
/// </para>
///
/// <para>
/// See also <see cref="HandlerAttribute" /> for marking message handlers and <see cref="SagaAttribute" /> for marking sagas
/// to enable source generation of the handler registry.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerRegistryExtensionsAttribute : Attribute
{
    /// <summary>
    /// The name of the entry point which must be a valid C# identifier. When not specified, defaults to the assembly name.
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
