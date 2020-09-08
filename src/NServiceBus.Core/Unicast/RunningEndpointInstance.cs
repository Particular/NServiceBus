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

        public async Task Stop(CancellationToken cancellationToken)
        {
            if (stopped)
            {
                return;
            }

            try
            {
                await stopSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                if (stopped)
                {
                    return;
                }

                Log.Info("Initiating shutdown.");

                // Cannot throw by design
                await receiveComponent.Stop(cancellationToken).ConfigureAwait(false);
                await featureComponent.Stop().ConfigureAwait(false);
                // Can throw
                await transportInfrastructure.Stop().ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                Log.Warn("Exception occurred during shutdown of the transport.", exception);
            }
            finally
            {
                settings.Clear();
                await hostingComponent.Stop().ConfigureAwait(false);

                stopped = true;
                Log.Info("Shutdown complete.");

                stopSemaphore.Release();
            }
        }

        public Task Send(object message, SendOptions options, CancellationToken cancellationToken)
        {
            return messageSession.Send(message, options, cancellationToken);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options, CancellationToken cancellationToken)
        {
            return messageSession.Send(messageConstructor, options, cancellationToken);
        }

        public Task Publish(object message, PublishOptions options, CancellationToken cancellationToken)
        {
            return messageSession.Publish(message, options, cancellationToken);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions, CancellationToken cancellationToken)
        {
            return messageSession.Publish(messageConstructor, publishOptions, cancellationToken);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageSession.Subscribe(eventType, options, cancellationToken);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options, CancellationToken cancellationToken)
        {
            return messageSession.Unsubscribe(eventType, options, cancellationToken);
        }

        HostingComponent hostingComponent;
        ReceiveComponent receiveComponent;
        FeatureComponent featureComponent;
        IMessageSession messageSession;
        readonly TransportInfrastructure transportInfrastructure;

        SettingsHolder settings;

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        static ILog Log = LogManager.GetLogger<RunningEndpointInstance>();
    }
}