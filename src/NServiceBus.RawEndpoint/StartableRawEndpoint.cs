namespace NServiceBus
{
    using System;
    using System.Threading.Tasks;
    using Config;
    using ConsistencyGuarantees;
    using Logging;
    using Settings;
    using Transport;
    using static MessageProcessingOptimizationExtensions;

    class StartableRawEndpoint
    {
        public StartableRawEndpoint(SettingsHolder settings, TransportInfrastructure transportInfrastructure, CriticalError criticalError, IPushMessages messagePump, IDispatchMessages dispatcher, Func<MessageContext, IDispatchMessages, Task> onMessage, Func<Task> preStartupCallback)
        {
            this.criticalError = criticalError;
            this.messagePump = messagePump;
            this.dispatcher = dispatcher;
            this.onMessage = onMessage;
            this.preStartupCallback = preStartupCallback;
            this.settings = settings;
            this.transportInfrastructure = transportInfrastructure;
        }

        public async Task<RunningRawEndpointInstance> Start()
        {
            await transportInfrastructure.Start().ConfigureAwait(false);

            await preStartupCallback().ConfigureAwait(false);

            var receiver = CreateReceiver();

            if (receiver != null)
            {
                await InitializeReceiver(receiver).ConfigureAwait(false);
            }

            var runningInstance = new RunningRawEndpointInstance(settings, receiver, transportInfrastructure, null);
            // set the started endpoint on CriticalError to pass the endpoint to the critical error action
            criticalError.SetEndpoint(runningInstance);

            if (receiver != null)
            {
                StartReceiver(receiver);
            }
            return runningInstance;
        }

        static void StartReceiver(RawTransportReceiver receiver)
        {
            try
            {
                receiver.Start();
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to start.", ex);
                throw;
            }
        }

        static async Task InitializeReceiver(RawTransportReceiver receiver)
        {
            try
            {
                await receiver.Init().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Fatal("Receiver failed to initialize.", ex);
                throw;
            }
        }

        RawTransportReceiver CreateReceiver()
        {
            if (settings.GetOrDefault<bool>("Endpoint.SendOnly"))
            {
                return null;
            }

            var purgeOnStartup = settings.GetOrDefault<bool>("Transport.PurgeOnStartup");
            var errorQueue = settings.ErrorQueueAddress();
            var requiredTransactionSupport = settings.GetRequiredTransactionModeForReceives();

            var receiver = BuildMainReceiver(errorQueue, purgeOnStartup, requiredTransactionSupport);

            return receiver;
        }

        RawTransportReceiver BuildMainReceiver(string errorQueue, bool purgeOnStartup, TransportTransactionMode requiredTransactionSupport)
        {
            var pushSettings = new PushSettings(settings.LocalAddress(), errorQueue, purgeOnStartup, requiredTransactionSupport);
            var dequeueLimitations = GetDequeueLimitationsForReceivePipeline();
            var errorHandlingPolicy = new RawEndpointErrorHandlingPolicy(settings, dispatcher, errorQueue);

            var receiver = new RawTransportReceiver(messagePump, dispatcher, onMessage, pushSettings, dequeueLimitations, criticalError, errorHandlingPolicy);
            return receiver;
        }

        //note: this should be handled in a feature but we don't have a good
        // extension point to plugin atm
        PushRuntimeSettings GetDequeueLimitationsForReceivePipeline()
        {
            ConcurrencyLimit concurrencyLimit;
            if (settings.TryGet(out concurrencyLimit))
            {
                return new PushRuntimeSettings(concurrencyLimit.MaxValue);
            }

            return PushRuntimeSettings.Default;
        }
        
        SettingsHolder settings;
        TransportInfrastructure transportInfrastructure;
        CriticalError criticalError;
        IPushMessages messagePump;
        IDispatchMessages dispatcher;
        Func<MessageContext, IDispatchMessages, Task> onMessage;
        Func<Task> preStartupCallback;

        static ILog Logger = LogManager.GetLogger<StartableRawEndpoint>();
    }
}