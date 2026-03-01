#nullable enable

namespace NServiceBus;

using System;

sealed class EndpointLifecycle(ExternallyManagedContainerHost externallyManagedContainerHost, IServiceProvider serviceProvider, object serviceKey, KeyedServiceCollectionAdapter services)
    : BaseEndpointLifecycle(externallyManagedContainerHost, serviceProvider)
{
    readonly IServiceProvider serviceProvider = serviceProvider;

    protected override IServiceProvider AdaptProvider(IServiceProvider provider, out IAsyncDisposable? lease)
    {
        var adaptedProvider = new KeyedServiceProviderAdapter(serviceProvider, serviceKey, services);
        lease = adaptedProvider;
        return adaptedProvider;
    }
}