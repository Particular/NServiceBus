#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Particular.Obsoletes;

/// <summary>
/// Provides factory methods for creating and starting endpoint instances.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint using Endpoint.Create or Endpoint.Start is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint using Endpoint.Create or Endpoint.Start is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public static class Endpoint
{
    /// <summary>
    /// Creates a new startable endpoint based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint using the Endpoint.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint using the Endpoint.Create method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        var serviceCollection = new ServiceCollection();
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);
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
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint using the Endpoint.Start method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "services.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint using the Endpoint.Start method is not recommended. Instead, consider using a hosting framework such as Generic Host or Web Host to manage the lifecycle of your endpoint. Use 'services.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var startableEndpoint = await Create(configuration, cancellationToken).ConfigureAwait(false);

        return await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
    }
}