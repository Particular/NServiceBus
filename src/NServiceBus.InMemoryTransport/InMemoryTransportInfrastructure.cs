namespace NServiceBus;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class InMemoryTransportInfrastructure : TransportInfrastructure
{
    public InMemoryTransportInfrastructure(HostSettings _, ReceiveSettings[] receivers, InMemoryTransport transport, InMemoryBroker broker)
    {
        Dispatcher = new InMemoryDispatcher(broker);
        this.broker = broker;
        this.transport = transport;

        Receivers = receivers.ToDictionary(
            r => r.Id,
            r => (IMessageReceiver)CreateReceiver(r));
    }

    InMemoryMessagePump CreateReceiver(ReceiveSettings receiveSettings)
    {
        var queueAddress = ToTransportAddress(receiveSettings.ReceiveAddress);

        ISubscriptionManager? subscriptionManager = receiveSettings.UsePublishSubscribe
            ? new InMemorySubscriptionManager(broker, queueAddress)
            : null;

        var pump = new InMemoryMessagePump(
            receiveSettings.Id,
            queueAddress,
            receiveSettings,
            transport.TransportTransactionMode,
            broker);

        pump.ConfigureSubscriptionManager(subscriptionManager);

        return pump;
    }

    readonly InMemoryTransport transport;
    readonly InMemoryBroker broker;

    public override Task Shutdown(CancellationToken cancellationToken = default) =>
        Task.WhenAll(Receivers.Values.Select(r => r.StopReceive(cancellationToken)));

    public override string ToTransportAddress(QueueAddress queueAddress)
    {
        var discriminator = queueAddress.Discriminator;
        var qualifier = queueAddress.Qualifier;

        return (string.IsNullOrEmpty(discriminator), string.IsNullOrEmpty(qualifier)) switch
        {
            (false, false) => $"{queueAddress.BaseAddress}-{discriminator}-{qualifier}",
            (false, true) => $"{queueAddress.BaseAddress}-{discriminator}",
            (true, false) => $"{queueAddress.BaseAddress}-{qualifier}",
            _ => queueAddress.BaseAddress
        };
    }
}
