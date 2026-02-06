#nullable enable
namespace NServiceBus;

using System;

/// <summary>
/// Marks a class as a message handler to enable source generation of the <see cref="HandlerRegistry">handler registry</see>.
/// </summary>
/// <remarks>
/// <para>
/// Although a message handler is identified as a class implementing <see cref="IHandleMessages&lt;T&gt;"/>, the class can further
/// be marked with the <see cref="HandlerAttribute" /> to enable source generation of the <see cref="HandlerRegistry" />
/// accessible via <see cref="EndpointConfiguration">endpointConfiguration</see>.<see cref="HandlerRegistry">Handlers</see>.
/// When marked with the attribute, the NServiceBus source generator creates methods to manually register the handler like
/// <c>endpointConfiguration.Handlers.MyAssemblyName.AddShipOrderHandler()</c> as well as convenience methods to register multiple handlers
/// like <c>endpointConfiguration.Handlers.MyAssemblyName.AddAll()</c>.
/// </para>
///
/// <para>
/// Once message handlers are manually registered through the <see cref="HandlerRegistry" />, an NServiceBus endpoint can be run
/// with assembly scanning disabled via
/// <c>endpointConfiguration.<see cref="AssemblyScannerConfigurationExtensions.AssemblyScanner">AssemblyScanner()</see>.<see cref="AssemblyScannerConfiguration.Disable">Disable</see> = true</c>.
/// Disabling assembly scanning can result in faster endpoint startup (especially in projects with a large number of assemblies)
/// and may be used to support assembly trimming and ahead-of-time compilation in a future version of NServiceBus.
/// </para>
///
/// <para>
/// An attribute is required to identify message handlers for source generation because source generators that analyze the full type hierarchy
/// are known to perform poorly, causing unacceptable slowdowns to project compilation and interaction with the IDE.
/// Automated code fixes are provided to make application of the HandlerAttribute and SagaAttribute to all relevant classes as easy as possible.
/// </para>
///
/// <para>See also <see cref="HandlerAttribute" /> for marking message handlers for source generation.</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerAttribute : Attribute;