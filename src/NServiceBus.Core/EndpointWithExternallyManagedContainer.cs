namespace NServiceBus;

using System;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Provides factory methods for creating endpoints instances with an externally managed container.
/// </summary>
public static class EndpointWithExternallyManagedContainer
{
    /// <summary>
    /// Creates a new startable endpoint based on the provided configuration that uses an externally managed container.
    /// </summary>
    public static IStartableEndpointWithExternallyManagedContainer Create(EndpointConfiguration configuration, IServiceCollection serviceCollection)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(serviceCollection);

        return CreateCore(configuration, serviceCollection);
    }

    internal static ExternallyManagedContainerHost CreateCore(EndpointConfiguration configuration,
        IServiceCollection serviceCollection)
    {
        var endpointLogSlot = configuration.GetSettings().Get<HostingComponent.Settings>().GetOrCreateEndpointLogSlot();

        using var _ = Logging.LogManager.BeginSlotScope(endpointLogSlot);
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);

        serviceCollection.TryAddSingleton<IMessageSession>(endpointCreator.MessageSession);

        return new ExternallyManagedContainerHost(endpointCreator);
    }
}