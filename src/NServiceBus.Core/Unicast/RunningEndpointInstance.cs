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
    IServiceProvider? serviceProvider) : IEndpointInstance
{
    public async Task Stop(CancellationToken cancellationToken = default)
    {
        if (status == Status.Stopped)
        {
            return;
        }

        var tokenRegistration = cancellationToken.Register(() => Log.Info("Aborting graceful shutdown."));

        await stoppingTokenSource.CancelAsync().ConfigureAwait(false);

        try
        {
            // Ensures to only continue if all parallel invocations can rely on the endpoint instance to be fully stopped.
            await stopSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            if (status >= Status.Stopping) // Another invocation is already handling Stop
            {
                return;
            }

            status = Status.Stopping;

            try
            {
                Log.Info("Initiating shutdown.");

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
            finally
            {
                settings.Clear();
                // When the service provider is externally managed the service provider is null
                if (serviceProvider is IAsyncDisposable asyncDisposableBuilder)
                {
                    await asyncDisposableBuilder.DisposeAsync().ConfigureAwait(false);
                }

                status = Status.Stopped;
                Log.Info("Shutdown complete.");
            }
        }
        finally
        {
            stopSemaphore.Release();

            await tokenRegistration.DisposeAsync().ConfigureAwait(false);

            stoppingTokenSource.Dispose();
        }
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