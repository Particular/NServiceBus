namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Logging;
    using Settings;
    using Transport;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(SettingsHolder settings, HostingComponent hostingComponent, ReceiveComponent receiveComponent, FeatureComponent featureComponent, IMessageSession messageSession, TransportInfrastructure transportInfrastructure, CancellationTokenSource stoppingTokenSource)
        {
            this.settings = settings;
            this.hostingComponent = hostingComponent;
            this.receiveComponent = receiveComponent;
            this.featureComponent = featureComponent;
            this.messageSession = messageSession;
            this.transportInfrastructure = transportInfrastructure;
            this.stoppingTokenSource = stoppingTokenSource;
        }

        public async Task Stop(CancellationToken cancellationToken = default)
        {
            if (status == Status.Stopped)
            {
                return;
            }

            cancellationToken.Register(() => Log.Info("Aborting graceful shutdown."));

            stoppingTokenSource.Cancel();

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
                    await featureComponent.Stop(cancellationToken).ConfigureAwait(false);

                    // Can throw
                    await transportInfrastructure.Shutdown(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex) when (!ex.IsCausedBy(cancellationToken))
                {
                    Log.Error("Shutdown of the transport infrastructure failed.", ex);

                    // TODO: Not throwing because reason unknown :)
                }
                finally
                {
                    settings.Clear();
                    hostingComponent.Stop();
                    status = Status.Stopped;
                    Log.Info("Shutdown complete.");
                }
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public Task Send(object message, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Send(message, sendOptions, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Send(messageConstructor, sendOptions, cancellationToken);
        }

        public Task Publish(object message, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Publish(message, publishOptions, cancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Publish(messageConstructor, publishOptions, cancellationToken);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Subscribe(eventType, subscribeOptions, cancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions, CancellationToken cancellationToken = default)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);

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

        HostingComponent hostingComponent;
        ReceiveComponent receiveComponent;
        FeatureComponent featureComponent;
        IMessageSession messageSession;
        readonly TransportInfrastructure transportInfrastructure;
        readonly CancellationTokenSource stoppingTokenSource;
        SettingsHolder settings;

        volatile Status status = Status.Running;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        static ILog Log = LogManager.GetLogger<RunningEndpointInstance>();

        enum Status
        {
            Running = 1,
            Stopping = 2,
            Stopped = 3
        }

#if NETCOREAPP
        ValueTask IAsyncDisposable.DisposeAsync() => new ValueTask(Stop());
#endif
    }
}