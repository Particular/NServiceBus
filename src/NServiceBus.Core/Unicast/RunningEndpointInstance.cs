namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using ObjectBuilder;
    using Settings;
    using Transport;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(SettingsHolder settings, IBuilder builder, ReceiveComponent receiveComponent, FeatureRunner featureRunner, IMessageSession messageSession, TransportInfrastructure transportInfrastructure)
        {
            this.settings = settings;
            this.builder = builder;
            this.receiveComponent = receiveComponent;
            this.featureRunner = featureRunner;
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
                await featureRunner.Stop(messageSession).ConfigureAwait(false);
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
                builder.Dispose();

                stopped = true;
                Log.Info("Shutdown complete.");

                stopSemaphore.Release();
            }
        }

        public Task Send(object message, SendOptions options)
        {
            return messageSession.Send(message, options);
        }

        public Task Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            return messageSession.Send(messageConstructor, options);
        }

        public Task Publish(object message, PublishOptions options)
        {
            return messageSession.Publish(message, options);
        }

        public Task Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return messageSession.Publish(messageConstructor, publishOptions);
        }

        public Task Subscribe(Type eventType, SubscribeOptions options)
        {
            return messageSession.Subscribe(eventType, options);
        }

        public Task Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            return messageSession.Unsubscribe(eventType, options);
        }

        IBuilder builder;
        ReceiveComponent receiveComponent;
        FeatureRunner featureRunner;
        IMessageSession messageSession;
        TransportInfrastructure transportInfrastructure;

        SettingsHolder settings;

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        static ILog Log = LogManager.GetLogger<RunningEndpointInstance>();
    }
}