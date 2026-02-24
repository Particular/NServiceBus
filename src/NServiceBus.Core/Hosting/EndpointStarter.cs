#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class EndpointStarter(
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    IServiceProvider serviceProvider,
    object serviceKey,
    KeyedServiceCollectionAdapter services) : IEndpointStarter
{
    public object LoggingSlot => serviceKey;

    public async ValueTask<IEndpointInstance> GetOrStart(CancellationToken cancellationToken = default)
    {
        if (endpoint != null)
        {
            return endpoint;
        }

        await startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (endpoint != null)
            {
                return endpoint;
            }

            LoggingBridge.RegisterMicrosoftFactoryIfAvailable(serviceProvider, LoggingSlot);
            using var _ = LoggingBridge.BeginScope(LoggingSlot);

            keyedServices = new KeyedServiceProviderAdapter(serviceProvider, serviceKey, services);

            endpoint = await startableEndpoint.Start(keyedServices, cancellationToken).ConfigureAwait(false);

            return endpoint;
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (endpoint == null || keyedServices == null)
        {
            return;
        }

        if (endpoint != null)
        {
            using var _ = LoggingBridge.BeginScope(LoggingSlot);
            await endpoint.Stop().ConfigureAwait(false);
        }

        if (keyedServices != null)
        {
            await keyedServices.DisposeAsync().ConfigureAwait(false);
        }
        startSemaphore.Dispose();
    }

    readonly SemaphoreSlim startSemaphore = new(1, 1);

    IEndpointInstance? endpoint;
    KeyedServiceProviderAdapter? keyedServices;
}