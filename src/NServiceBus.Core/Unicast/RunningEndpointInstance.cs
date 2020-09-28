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
            if (stopped)
            {
                return;
            }

            try
            {
                await stopSemaphore.WaitAsync().ConfigureAwait(false);

                if (stopped)
                {
                    return;
                }

                Log.Info("Initiating shutdown.");

                // Cannot throw by design
                await receiveComponent.Stop().ConfigureAwait(false);
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

        public Task Send(object message, SendOptions sendOptions)
        {
            return messageSession.Send(message, sendOptions);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions sendOptions)
        {
            return messageSession.Send(messageConstructor, sendOptions);
        }

        public Task Publish(object message, PublishOptions publishOptions)
        {
            return messageSession.Publish(message, publishOptions);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return messageSession.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions subscribeOptions)
        {
            return messageSession.Subscribe(eventType, subscribeOptions);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions unsubscribeOptions)
        {
            return messageSession.Unsubscribe(eventType, unsubscribeOptions);
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