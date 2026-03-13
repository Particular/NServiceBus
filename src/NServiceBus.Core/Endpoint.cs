#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Provides factory methods for creating and starting endpoint instances.
/// </summary>
public static class Endpoint
{
    /// <summary>
    /// Creates a new startable endpoint based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var serviceCollection = new ServiceCollection();
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);
        configuration.ApplyUserServicesTo(serviceCollection);
        var serviceProvider = serviceCollection.BuildServiceProvider();

        var host = new InternallyManagedContainerHost(endpointCreator, serviceProvider);
        await host.Create(cancellationToken).ConfigureAwait(false);
        return host;
    }

    /// <summary>
    /// Creates and starts a new endpoint based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var startableEndpoint = await Create(configuration, cancellationToken).ConfigureAwait(false);

        return await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
    }
}