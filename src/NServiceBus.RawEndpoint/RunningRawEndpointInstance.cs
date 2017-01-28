namespace NServiceBus
{
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using Logging;
    using Settings;
    using Transport;

    class RunningRawEndpointInstance : IRawEndpointInstance
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
    }
}