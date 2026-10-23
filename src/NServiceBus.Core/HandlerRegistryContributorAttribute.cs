#nullable enable
namespace NServiceBus;

using System;
using System.ComponentModel;

/// <summary>
/// Marks a type that contributes handlers and sagas to the generated handler registry of the assembly.
/// </summary>
/// <remarks>
/// Source generators emit this attribute on a class that implements <see cref="IHandlerRegistryContributor{TSelf}" /> or <see cref="IHandlerRegistryContributor" />.
/// The type can be internal. It must not be generic and must be accessible from the generated registry.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class HandlerRegistryContributorAttribute : Attribute
{
}
