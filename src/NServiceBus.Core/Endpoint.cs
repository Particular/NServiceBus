#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Particular.Obsoletes;

/// <summary>
/// Provides factory methods for creating and starting endpoint instances.
/// </summary>
[ObsoleteMetadata(
    Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
    TreatAsErrorFromVersion = "11",
    RemoveInVersion = "12",
    ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
[Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
public static class Endpoint
{
    /// <summary>
    /// Creates a new startable endpoint based on the provided configuration.
    /// </summary>
    /// <param name="configuration">Configuration.</param>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe.</param>
    [ObsoleteMetadata(
        Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task<IStartableEndpoint> Create(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        // Backdoor for acceptance testing
        var keyedServices = configuration.GetSettings().GetOrDefault<KeyedServiceCollectionAdapter>();
        IServiceCollection serviceCollection = keyedServices is not null ? keyedServices : new ServiceCollection();
        var endpointCreator = EndpointCreator.Create(configuration, serviceCollection);
        serviceCollection.TryAddSingleton<IMessageSession>(endpointCreator.MessageSession);

        // Backdoor for acceptance testing
        IServiceProvider serviceProvider = keyedServices is not null ? new KeyedServiceProviderAdapter(keyedServices.Inner.BuildServiceProvider(), keyedServices.ServiceKey, keyedServices, ownsProvider: true) : serviceCollection.BuildServiceProvider();

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
        Message = "Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint",
        TreatAsErrorFromVersion = "11",
        RemoveInVersion = "12",
        ReplacementTypeOrMember = "IServiceCollection.AddNServiceBusEndpoint")]
    [Obsolete("Self-hosting an endpoint is no longer recommended. Instead, consider using a Microsoft IHostApplicationBuilder-based host to manage the lifecycle of your endpoint. Use 'IServiceCollection.AddNServiceBusEndpoint' instead. Will be treated as an error from version 11.0.0. Will be removed in version 12.0.0.", false)]
    public static async Task<IEndpointInstance> Start(EndpointConfiguration configuration, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var startableEndpoint = await Create(configuration, cancellationToken).ConfigureAwait(false);

        return await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
    }
}