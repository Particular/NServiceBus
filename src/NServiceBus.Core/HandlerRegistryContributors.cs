#nullable enable
namespace NServiceBus;

using System.ComponentModel;

/// <summary>
/// Calls <see cref="IHandlerRegistryContributor" /> implementations. Should only be called by a source generator.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class HandlerRegistryContributors
{
    /// <summary>
    /// Registers the handlers of the contributor with the endpoint configuration.
    /// </summary>
    public static void AddHandlers<TContributor>(EndpointConfiguration configuration)
        where TContributor : IHandlerRegistryContributor =>
        TContributor.Instance.AddHandlers(configuration);

    /// <summary>
    /// Registers the sagas of the contributor with the endpoint configuration.
    /// </summary>
    public static void AddSagas<TContributor>(EndpointConfiguration configuration)
        where TContributor : IHandlerRegistryContributor =>
        TContributor.Instance.AddSagas(configuration);
}
