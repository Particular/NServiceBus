namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Settings;
    using Transport;
    using UnicastBus = Unicast.UnicastBus;

    class RunningRawEndpointInstance : IEndpointInstance
    {
        public RunningRawEndpointInstance(SettingsHolder settings, RawTransportReceiver receiver, TransportInfrastructure transportInfrastructure, IDispatchMessages dispatcher)
        {
            this.settings = settings;
            this.receiver = receiver;
            this.transportInfrastructure = transportInfrastructure;
            this.dispatcher = dispatcher;
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

                await StopReceiver().ConfigureAwait(false);
                await transportInfrastructure.Stop().ConfigureAwait(false);
                settings.Clear();

                stopped = true;
                Log.Info("Shutdown complete.");
            }
            finally
            {
                stopSemaphore.Release();
            }
        }

        public Task SendRaw(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, context);
        }

        async Task StopReceiver()
        {
            Log.Debug("Stopping receiver");
            await receiver.Stop().ConfigureAwait(false);
            Log.Debug("Stopped receiver");
        }

        TransportInfrastructure transportInfrastructure;
        IDispatchMessages dispatcher;

        SettingsHolder settings;
        RawTransportReceiver receiver;

        volatile bool stopped;
        SemaphoreSlim stopSemaphore = new SemaphoreSlim(1);

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();

        #region IEndpointInstance
        Task IMessageSession.Send(object message, SendOptions options)
        {
            throw new NotImplementedException();
        }

        Task IMessageSession.Send<T>(Action<T> messageConstructor, SendOptions options)
        {
            throw new NotImplementedException();
        }

        Task IMessageSession.Publish(object message, PublishOptions options)
        {
            throw new NotImplementedException();
        }

        Task IMessageSession.Publish<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            throw new NotImplementedException();
        }

        Task IMessageSession.Subscribe(Type eventType, SubscribeOptions options)
        {
            throw new NotImplementedException();
        }

        Task IMessageSession.Unsubscribe(Type eventType, UnsubscribeOptions options)
        {
            throw new NotImplementedException();
        }
        #endregion
    }
}