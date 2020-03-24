using System.Threading.Tasks;
using NServiceBus.Extensibility;
using NServiceBus.Logging;
using NServiceBus.Settings;
using NServiceBus.Transport;

namespace NServiceBus.Raw
{
    class RunningRawEndpointInstance : IReceivingRawEndpoint
    {
        public RunningRawEndpointInstance(SettingsHolder settings, RawTransportReceiver receiver, TransportInfrastructure transportInfrastructure, IDispatchMessages dispatcher, IManageSubscriptions subscriptionManager, string localAddress)
        {
            this.settings = settings;
            this.receiver = receiver;
            this.transportInfrastructure = transportInfrastructure;
            this.dispatcher = dispatcher;
            this.TransportAddress = localAddress;
            SubscriptionManager = subscriptionManager;
        }

        public Task Dispatch(TransportOperations outgoingMessages, TransportTransaction transaction, ContextBag context)
        {
            return dispatcher.Dispatch(outgoingMessages, transaction, context);
        }

        public string ToTransportAddress(LogicalAddress logicalAddress)
        {
            return transportInfrastructure.ToTransportAddress(logicalAddress);
        }

        public async Task<IStoppableRawEndpoint> StopReceiving()
        {
            if (receiver != null)
            {
                Log.Info("Stopping receiver.");
                await receiver.Stop().ConfigureAwait(false);
                Log.Info("Receiver stopped.");
            }
            return new StoppableRawEndpoint(transportInfrastructure, settings);
        }

        public string TransportAddress { get; }
        public string EndpointName => settings.EndpointName();
        public ReadOnlySettings Settings => settings;
        public IManageSubscriptions SubscriptionManager { get; }

        public async Task Stop()
        {
            var stoppable = await StopReceiving().ConfigureAwait(false);
            await stoppable.Stop();
        }

        TransportInfrastructure transportInfrastructure;
        IDispatchMessages dispatcher;

        SettingsHolder settings;
        RawTransportReceiver receiver;

        static ILog Log = LogManager.GetLogger<RunningRawEndpointInstance>();
    }
}