#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class BaseEndpointLifecycle(
    ExternallyManagedContainerHost externallyManagedContainerHost,
    IServiceProvider serviceProvider) : IEndpointLifecycle
{
    public async ValueTask Create(bool forceInstallers = false, CancellationToken cancellationToken = default)
    {
        if (startableEndpoint != null)
        {
            return;
        }

        await lifeCycleSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (startableEndpoint is not null)
            {
                return;
            }

            startableEndpoint = await externallyManagedContainerHost.Create(AdaptProvider(serviceProvider, out providerLease), forceInstallers, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            lifeCycleSemaphore.Release();
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

        if (endpointInstance != null)
        {
            return;
        }

        await lifeCycleSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (endpointInstance != null)
            {
                return;
            }

            endpointInstance = await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lifeCycleSemaphore.Release();
        }
    }

    public async ValueTask<RunningEndpointInstance> CreateAndStart(CancellationToken cancellationToken = default)
    {
        await Create(cancellationToken: cancellationToken).ConfigureAwait(false);
        await Start(cancellationToken).ConfigureAwait(false);
        return endpointInstance ?? throw new InvalidOperationException("The endpoint instance should have been created and started at this point.");
    }

    public async ValueTask Stop(CancellationToken cancellationToken = default)
    {
        if (Interlocked.CompareExchange(ref isStopped, 1, 0) == 1)
        {
            return;
        }

        // The semaphore is an internal serialization mechanism; the caller's token
        // must not be able to abort the wait. A failed wait leaves endpointInstance
        // non-null, allowing a subsequent DisposeAsync -> Stop(None) re-entry to
        // attempt full shutdown against a DI container that is already torn down.
        await lifeCycleSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (endpointInstance is null)
            {
                return;
            }

            await endpointInstance.Stop(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            lifeCycleSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) == 1)
        {
            return;
        }

        var instance = endpointInstance;
        endpointInstance = null;

        // DisposeAsync calls Stop internally, which is idempotent. This ensures
        // cleanup runs even if Stop was never called or threw.
        if (instance is not null)
        {
            await instance.DisposeAsync().ConfigureAwait(false);
        }

        if (providerLease is not null)
        {
            await providerLease.DisposeAsync().ConfigureAwait(false);
        }
    }

    readonly SemaphoreSlim lifeCycleSemaphore = new(1, 1);

    volatile StartableEndpoint? startableEndpoint;
    RunningEndpointInstance? endpointInstance;
    IAsyncDisposable? providerLease;
    int isDisposed;
    int isStopped;
}