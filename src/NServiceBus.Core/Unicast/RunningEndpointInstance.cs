namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Features;
    using Logging;
    using ObjectBuilder;
    using Settings;
    using Transport;
    using UnicastBus = Unicast.UnicastBus;

    class RunningEndpointInstance : IEndpointInstance
    {
        public RunningEndpointInstance(SettingsHolder settings, IBuilder builder, List<TransportReceiver> receivers, FeatureRunner featureRunner, IMessageSession messageSession, TransportInfrastructure transportInfrastructure)
        {
            this.settings = settings;
            this.builder = builder;
            this.receivers = receivers;
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

            await stopSemaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                if (stopped)
                {
                    return;
                }

                Log.Info("Initiating shutdown.");

                await StopReceivers().ConfigureAwait(false);
                await featureRunner.Stop(messageSession).ConfigureAwait(false);
                await transportInfrastructure.Stop().ConfigureAwait(false);
                settings.Clear();
                builder.Dispose();

                stopped = true;
                Log.Info("Shutdown complete.");
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public Task Send(object message, SendOptions options)
        {
            return messageSession.Send(message, options);
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

        Task StopReceivers()
        {
            var receiverStopTasks = receivers.Select(async receiver =>
            {
                Log.DebugFormat("Stopping {0} receiver", receiver.Id);
                await receiver.Stop().ConfigureAwait(false);
                Log.DebugFormat("Stopped {0} receiver", receiver.Id);
            });

            return Task.WhenAll(receiverStopTasks);
        }

        IBuilder builder;
        List<TransportReceiver> receivers;
        FeatureRunner featureRunner;
        IMessageSession messageSession;
        TransportInfrastructure transportInfrastructure;

        SettingsHolder settings;

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        static ILog Log = LogManager.GetLogger<UnicastBus>();
    }
}