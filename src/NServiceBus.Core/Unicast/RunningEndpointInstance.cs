#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Features;
using Logging;
using Settings;
using Transport;

class RunningEndpointInstance(SettingsHolder settings,
    ReceiveComponent receiveComponent,
    FeatureComponent featureComponent,
    IMessageSession messageSession,
    TransportInfrastructure transportInfrastructure,
    CancellationTokenSource stoppingTokenSource,
    IAsyncDisposable serviceProviderLease,
#pragma warning disable CS0618 // Type or member is obsolete -- Change the interface to IMessageSession in the next major
    LogSlot endpointLogSlot,
    TimeSpan? disposeShutdownTimeout = null) : IEndpointInstance, IAsyncDisposable
#pragma warning restore CS0618 // Type or member is obsolete
{
    // Bound the shutdown wait inside DisposeAsync. DisposeAsync is reached when an IHost
    // is disposed without an explicit StopAsync (e.g. `using var host` / `await using var
    // host` followed by scope exit), so no host shutdown token reaches us. Mirrors
    // HostOptions.ShutdownTimeout's default of 30s.
    readonly TimeSpan disposeShutdownTimeout = disposeShutdownTimeout ?? TimeSpan.FromSeconds(30);
    // Stop is the legacy interface for shutting down the endpoint over the public API.
    // The modern hosted variant has Stop and DisposeAsync as separate steps:
    // BaseEndpointLifecycle.Stop calls StopCore (shutdown only), then DisposeAsync (cleanup).
    // The legacy Stop implementation must call DisposeAsync to ensure the endpoint is fully
    // shut down and resources are released.
    public async Task Stop(CancellationToken cancellationToken = default)
    {
        try
        {
            await StopCore(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            await DisposeAsync().ConfigureAwait(false);
        }
    }

    public async Task StopCore(CancellationToken cancellationToken = default)
    {
        if (status >= Status.Stopping)
        {
            return;
        }

        // The first caller to enter StopCore owns shutdown. Later callers that observe
        // Stopping or Stopped return immediately without waiting. The semaphore is an
        // internal serialization mechanism; the caller's token must not be able to abort
        // the wait because a failed wait leaves status as Running, allowing a subsequent
        // DisposeAsync -> StopCore re-entry to attempt full shutdown against a DI
        // container that is already torn down.
        await stopSemaphore.WaitAsync(CancellationToken.None).ConfigureAwait(false);

        try
        {
            if (status >= Status.Stopping)
            {
                return;
            }

            status = Status.Stopping;

            using var _ = LogManager.BeginSlotScope(endpointLogSlot);
            await using var tokenRegistration = cancellationToken.Register(() => Log.Info("Aborting graceful shutdown."))
                .ConfigureAwait(false);
            Log.Info("Initiating shutdown.");

            await stoppingTokenSource.CancelAsync().ConfigureAwait(false);

            try
            {
                // Cannot throw by design
                await receiveComponent.Stop(cancellationToken).ConfigureAwait(false);
                await featureComponent.StopFeatures(messageSession, cancellationToken).ConfigureAwait(false);

                // Can throw
                await transportInfrastructure.Shutdown(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
            {
                Log.Error("Shutdown of the transport infrastructure failed.", ex);
            }

            Log.Info("Shutdown complete.");
        }
        finally
        {
            status = Status.Stopped;
            stopSemaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref isDisposed, 1) == 1)
        {
            return;
        }

        using var disposeCancelSource = new CancellationTokenSource(disposeShutdownTimeout);
        var disposeCancellationToken = disposeCancelSource.Token;
        try
        {
            await StopCore(disposeCancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (disposeCancellationToken.IsCancellationRequested)
        {
            Log.Warn($"Graceful shutdown timed out after {disposeShutdownTimeout} during DisposeAsync; continuing with resource cleanup.");
        }

        settings.Clear();
        stoppingTokenSource.Dispose();
        await serviceProviderLease.DisposeAsync().ConfigureAwait(false);
    }

    public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(sendOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Send(message, sendOptions, cancellationToken);
    }

    public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(sendOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Send(messageConstructor, sendOptions, cancellationToken);
    }

    public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);
        ArgumentNullException.ThrowIfNull(publishOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Publish(message, publishOptions, cancellationToken);
    }

    public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(messageConstructor);
        ArgumentNullException.ThrowIfNull(publishOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Publish(messageConstructor, publishOptions, cancellationToken);
    }

    public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(subscribeOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Subscribe(eventType, subscribeOptions, cancellationToken);
    }

    public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(unsubscribeOptions);

        GuardAgainstUseWhenNotStarted();
        return messageSession.Unsubscribe(eventType, unsubscribeOptions, cancellationToken);
    }

    void GuardAgainstUseWhenNotStarted()
    {
        if (status >= Status.Stopping)
        {
            throw new InvalidOperationException("Invoking messaging operations on the endpoint instance after it has been triggered to stop is not supported.");
        }
    }

    int isDisposed;
    volatile Status status = Status.Running;
    readonly SemaphoreSlim stopSemaphore = new(1);

    static readonly ILog Log = LogManager.GetLogger<RunningEndpointInstance>();

    enum Status
    {
        Running = 1,
        Stopping = 2,
        Stopped = 3
    }
}