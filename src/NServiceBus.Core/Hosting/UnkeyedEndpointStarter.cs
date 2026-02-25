#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class UnkeyedEndpointStarter(
    IStartableEndpointWithExternallyManagedContainer startableEndpoint,
    IServiceProvider serviceProvider,
    object loggingSlot) : IEndpointStarter
{
    public object LoggingSlot => loggingSlot;

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

            endpoint = await startableEndpoint.Start(serviceProvider, cancellationToken).ConfigureAwait(false);

            return endpoint;
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (endpoint == null)
        {
            return;
        }

        await endpoint.Stop().ConfigureAwait(false);
        startSemaphore.Dispose();
    }

    readonly SemaphoreSlim startSemaphore = new(1, 1);

    IEndpointInstance? endpoint;
}