#nullable enable
namespace NServiceBus;

using System.ComponentModel;

/// <summary>
/// Contributes handlers and sagas to the generated handler registry from outside the NServiceBus source generators.
/// </summary>
/// <remarks>
/// <para>
/// Source generators in other packages implement this interface on a generated type and declare it with the
/// <see cref="HandlerRegistryContributorAttribute" />. The implementation must be public and implicit, and must not be generic.
/// </para>
/// <para>
/// The namespace of the implementing type selects the registry node whose <c>AddAll</c>, <c>AddAllHandlers</c>, and
/// <c>AddAllSagas</c> methods call it, following the same namespace rules as handlers and sagas.
/// </para>
/// <para>
/// Implementations must register handlers and sagas without reflection to keep the endpoint trimming and Native AOT compatible.
/// </para>
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public interface IHandlerRegistryContributor
{
    /// <summary>
    /// Registers the handlers of the contributor with the endpoint configuration.
    /// </summary>
    static abstract void AddHandlers(EndpointConfiguration configuration);

    /// <summary>
    /// Registers the sagas of the contributor with the endpoint configuration.
    /// </summary>
    static abstract void AddSagas(EndpointConfiguration configuration);
}
