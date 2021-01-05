using System.Linq;
using System.Threading;
using NServiceBus.Transport;

namespace NServiceBus.AcceptanceTests.Core.FakeTransport
{
    using System.Threading.Tasks;
    using Transport;

    public class FakeTransportInfrastructure : TransportInfrastructure
    {
        readonly FakeTransport.StartUpSequence startUpSequence;
        readonly HostSettings hostSettings;
        readonly ReceiveSettings[] receivers;
        readonly string[] sendingAddresses;
        readonly CancellationToken cancellationToken;
        readonly FakeTransport transportSettings;

        public FakeTransportInfrastructure(FakeTransport.StartUpSequence startUpSequence, HostSettings hostSettings,
            ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken,
            FakeTransport transportSettings)
        {
            this.startUpSequence = startUpSequence;
            this.hostSettings = hostSettings;
            this.receivers = receivers;
            this.sendingAddresses = sendingAddresses;
            this.cancellationToken = cancellationToken;
            this.transportSettings = transportSettings;
        }

        public void ConfigureReceiveInfrastructure()
        {
            Receivers = receivers
                .Select(r => 
                    new FakeReceiver(
                        r.Id, 
                        transportSettings, 
                        startUpSequence, 
                        hostSettings.CriticalErrorAction))
                .ToList<IMessageReceiver>()
                .AsReadOnly();
        }

        public void ConfigureSendInfrastructure()
        {
            Dispatcher = new FakeDispatcher();
        }

        public override Task DisposeAsync()
        {
            startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(DisposeAsync)}");

            if (transportSettings.ErrorOnTransportDispose != null)
            {
                throw transportSettings.ErrorOnTransportDispose;
            }

            return Task.CompletedTask;
        }
    }
}