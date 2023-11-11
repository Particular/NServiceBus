namespace NServiceBus;

using System;
using Microsoft.Extensions.DependencyInjection;

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

        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);
        return new ExternallyManagedContainerHost(endpointCreator);
    }
}