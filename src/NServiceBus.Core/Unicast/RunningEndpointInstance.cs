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
        public RunningEndpointInstance(SettingsHolder settings, HostingComponent hostingComponent, ReceiveComponent receiveComponent, FeatureComponent featureComponent, IMessageSession messageSession, TransportInfrastructure transportInfrastructure)
        {
            this.settings = settings;
            this.hostingComponent = hostingComponent;
            this.receiveComponent = receiveComponent;
            this.featureComponent = featureComponent;
            this.messageSession = messageSession;
            this.transportInfrastructure = transportInfrastructure;
        }

        public async Task Stop()
        {
            if (status == Status.Stopped)
            {
                return;
            }

            try
            {
                // Ensures to only continue if all parallel invocations can rely on the endpoint instance to be fully stopped.
                await stopSemaphore.WaitAsync().ConfigureAwait(false);

                if (status >= Status.Stopping) // Another invocation is already handling Stop
                {
                    return;
                }

                status = Status.Stopping;

                try
                {
                    Log.Info("Initiating shutdown.");

                    // Cannot throw by design
                    await receiveComponent.Stop().ConfigureAwait(false);
                    await featureComponent.Stop().ConfigureAwait(false);

                    // Can throw
                    await transportInfrastructure.DisposeAsync().ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    Log.Error("Shutdown of the transport infrastructure failed.", exception);

                    // TODO: Not throwing because reason unknown :)
                }
                finally
                {
                    settings.Clear();
                    await hostingComponent.Stop().ConfigureAwait(false);
                    status = Status.Stopped;
                    Log.Info("Shutdown complete.");
                }
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public Task Send(object message, SendOptions sendOptions)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Send(message, sendOptions);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(sendOptions), sendOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Send(messageConstructor, sendOptions);
        }

        public Task Publish(object message, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(message), message);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Publish(message, publishOptions);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            Guard.AgainstNull(nameof(messageConstructor), messageConstructor);
            Guard.AgainstNull(nameof(publishOptions), publishOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(subscribeOptions), subscribeOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Subscribe(eventType, subscribeOptions);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions)
        {
            Guard.AgainstNull(nameof(eventType), eventType);
            Guard.AgainstNull(nameof(unsubscribeOptions), unsubscribeOptions);

            GuardAgainstUseWhenNotStarted();
            return messageSession.Unsubscribe(eventType, unsubscribeOptions);
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
    }
}