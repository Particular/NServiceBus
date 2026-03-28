namespace NServiceBus.TransportTests;

using System.Threading;
using System.Threading.Tasks;
using Transport;

class ConfigureInMemoryTransportInfrastructure : IConfigureTransportInfrastructure
{
    public TransportDefinition CreateTransportDefinition() => new InMemoryTransport();

    public async Task<TransportInfrastructure> Configure(TransportDefinition transportDefinition, HostSettings hostSettings, QueueAddress inputQueueName, string errorQueueName, CancellationToken cancellationToken = default)
    {
        var mainReceiverSettings = new ReceiveSettings(
            "mainReceiver",
            inputQueueName,
            transportDefinition.SupportsPublishSubscribe,
            true,
            errorQueueName);

        var transportInfrastructure = await transportDefinition.Initialize(
            hostSettings,
            [mainReceiverSettings],
            [errorQueueName],
            cancellationToken).ConfigureAwait(false);

        return transportInfrastructure;
    }

    public Task Cleanup(CancellationToken cancellationToken = default) => Task.CompletedTask;
}