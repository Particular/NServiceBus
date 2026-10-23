#nullable enable
namespace NServiceBus;

using System;
using System.ComponentModel;

/// <summary>
/// Declares a type that contributes handlers and sagas to the generated handler registry of the assembly.
/// </summary>
/// <remarks>
/// Source generators emit this attribute together with a type implementing <see cref="IHandlerRegistryContributor" />.
/// The type can be internal. It must be accessible from the assembly that declares the attribute.
/// </remarks>
/// <param name="contributorType">The type that implements <see cref="IHandlerRegistryContributor" />.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = true)]
public sealed class HandlerRegistryContributorAttribute(Type contributorType) : Attribute
{
    /// <summary>
    /// The type that implements <see cref="IHandlerRegistryContributor" />.
    /// </summary>
    public Type ContributorType { get; } = contributorType ?? throw new ArgumentNullException(nameof(contributorType));
}
