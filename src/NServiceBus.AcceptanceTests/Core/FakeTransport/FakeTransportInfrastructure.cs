namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Linq;
    using Transport;
    using System.Threading.Tasks;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        readonly FakeTransport.StartUpSequence startUpSequence;
        readonly HostSettings hostSettings;
        readonly ReceiveSettings[] receivers;
        readonly string[] sendingAddresses;
        readonly FakeTransport transportSettings;

        public FakeTransportInfrastructure(FakeTransport.StartUpSequence startUpSequence, HostSettings hostSettings,
            ReceiveSettings[] receivers, string[] sendingAddresses, FakeTransport transportSettings)
        {
            this.startUpSequence = startUpSequence;
            this.hostSettings = hostSettings;
            this.receivers = receivers;
            this.sendingAddresses = sendingAddresses;
            this.transportSettings = transportSettings;
        }

        public void ConfigureReceiveInfrastructure() =>
            Receivers = receivers
                .Select(r =>
                    new FakeReceiver(
                        r.Id,
                        transportSettings,
                        startUpSequence,
                        hostSettings.CriticalErrorAction))
                .ToDictionary<FakeReceiver, string, IMessageReceiver>(r => r.Id, r => r);

        public void ConfigureSendInfrastructure()
        {
            Dispatcher = new FakeDispatcher();
        }

        public override Task Shutdown()
        {
            startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(Shutdown)}");

            if (transportSettings.ErrorOnTransportDispose != null)
            {
                throw transportSettings.ErrorOnTransportDispose;
            }

            return Task.CompletedTask;
        }
    }
}