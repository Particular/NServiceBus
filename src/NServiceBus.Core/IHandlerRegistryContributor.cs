#nullable enable
namespace NServiceBus;

using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

/// <summary>
/// Contributes handlers and sagas to the generated handler registry from outside the NServiceBus source generators.
/// </summary>
/// <remarks>
/// <para>
/// Source generators in other packages implement <see cref="IHandlerRegistryContributor{TSelf}" /> on a generated class and mark
/// the class with the <see cref="HandlerRegistryContributorAttribute" />.
/// </para>
/// <para>
/// The namespace of the implementing type selects the registry node whose <c>AddAll</c>, <c>AddAllHandlers</c>, and
/// <c>AddAllSagas</c> methods call it, following the same namespace rules as handlers and sagas.
/// </para>
/// <para>
/// Implementations must register handlers and sagas without reflection to keep the endpoint trimming and Native AOT compatible.
/// The registrations can run more than once, for example when an endpoint registers an assembly explicitly and then calls <c>Handlers.AddAll()</c>.
/// </para>
/// <para>
/// Because of their static abstract members, this interface and <see cref="IHandlerRegistryContributor{TSelf}" /> cannot be used as type arguments. Use them only as generic constraints.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHandlerRegistryContributor
{
    /// <summary>
    /// Gets the contributor instance.
    /// </summary>
    static abstract IHandlerRegistryContributor Instance { get; }

    /// <summary>
    /// Registers the handlers of the contributor with the endpoint configuration.
    /// </summary>
    void AddHandlers(EndpointConfiguration configuration);

    /// <summary>
    /// Registers the sagas of the contributor with the endpoint configuration.
    /// </summary>
    void AddSagas(EndpointConfiguration configuration);
}

/// <summary>
/// Contributes handlers and sagas to the generated handler registry and exposes the contributor instance with its own type.
/// </summary>
/// <remarks>
/// Implementing this interface satisfies <see cref="IHandlerRegistryContributor" />. Implement <see cref="Instance" /> implicitly or explicitly.
/// </remarks>
/// <typeparam name="TSelf">The implementing type.</typeparam>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHandlerRegistryContributor<TSelf> : IHandlerRegistryContributor
    where TSelf : IHandlerRegistryContributor<TSelf>
{
    /// <summary>
    /// Gets the contributor instance.
    /// </summary>
    [NotNull]
    static new abstract TSelf Instance { get; }

    static IHandlerRegistryContributor IHandlerRegistryContributor.Instance => TSelf.Instance;
}
