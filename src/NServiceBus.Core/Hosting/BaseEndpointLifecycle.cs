#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class BaseEndpointLifecycle(
    ExternallyManagedContainerHost externallyManagedContainerHost,
    IServiceProvider serviceProvider) : IEndpointLifecycle
{
    public async ValueTask Create(CancellationToken cancellationToken = default)
    {
        if (startableEndpoint != null)
        {
            return;
        }

        await startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (startableEndpoint is not null)
            {
                return;
            }

            startableEndpoint = await externallyManagedContainerHost.Create(AdaptProvider(serviceProvider, out providerLease), cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    protected virtual IServiceProvider AdaptProvider(IServiceProvider provider, out IAsyncDisposable? lease)
    {
        lease = null;
        return provider;
    }

    public async ValueTask Start(CancellationToken cancellationToken = default)
    {
        if (startableEndpoint is null)
        {
            throw new InvalidOperationException("The endpoint must be created before it can be started.");
        }

        if (endpointInstance is not null)
        {
            return;
        }

        await startSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (endpointInstance is not null)
            {
                return;
            }

            endpointInstance = await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            startSemaphore.Release();
        }
    }

    public async ValueTask<IEndpointInstance> CreateAndStart(CancellationToken cancellationToken = default)
    {
        await Create(cancellationToken).ConfigureAwait(false);
        await Start(cancellationToken).ConfigureAwait(false);
        return endpointInstance ?? throw new InvalidOperationException("The endpoint instance should have been created and started at this point.");
    }

    public async ValueTask DisposeAsync()
    {
        if (endpointInstance == null)
        {
            return;
        }

        try
        {
            await endpointInstance.Stop().ConfigureAwait(false);
        }
        finally
        {
            if (providerLease is not null)
            {
                await providerLease.DisposeAsync().ConfigureAwait(false);
            }

            startSemaphore.Dispose();
        }
    }

    readonly SemaphoreSlim startSemaphore = new(1, 1);

    StartableEndpoint? startableEndpoint;
    IEndpointInstance? endpointInstance;
    IAsyncDisposable? providerLease;
}