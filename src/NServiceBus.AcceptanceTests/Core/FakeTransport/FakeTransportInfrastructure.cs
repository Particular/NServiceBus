using System.Linq;
using System.Threading;
using NServiceBus.Transports;

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

        public FakeTransportInfrastructure(FakeTransport.StartUpSequence startUpSequence, HostSettings hostSettings,
            ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken)
        {
            this.startUpSequence = startUpSequence;
            this.hostSettings = hostSettings;
            this.receivers = receivers;
            this.sendingAddresses = sendingAddresses;
            this.cancellationToken = cancellationToken;
        }

        public void ConfigureReceiveInfrastructure()
        {
            startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(ConfigureReceiveInfrastructure)}");

            Receivers = receivers.Select(r => new FakeReceiver(r.Id)).ToList<IMessageReceiver>().AsReadOnly();
        }

        public void ConfigureSendInfrastructure()
        {
            startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(ConfigureSendInfrastructure)}");

            Dispatcher = new FakeDispatcher();
        }

        public override Task DisposeAsync()
        {
            startUpSequence.Add($"{nameof(TransportInfrastructure)}.{nameof(DisposeAsync)}");

            return Task.CompletedTask;
        }
    }
}