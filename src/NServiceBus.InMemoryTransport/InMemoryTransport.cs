namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Transport;

/// <summary>
/// In-memory transport for testing and development.
/// </summary>
public class InMemoryTransport : TransportDefinition
{
    /// <summary>
    /// Creates a new instance of the in-memory transport using a shared broker.
    /// </summary>
    /// <remarks>
    /// When multiple endpoints need to communicate in-memory, they should share the same broker instance.
    /// For testing, omit the broker parameter and a shared instance will be used.
    /// </remarks>
    public InMemoryTransport(InMemoryBroker? broker = null)
        : base(TransportTransactionMode.SendsAtomicWithReceive, supportsDelayedDelivery: true, supportsPublishSubscribe: true, supportsTTBR: true)
    {
        configuredBroker = broker;
    }

    internal InMemoryBroker GetBroker() => configuredBroker ?? SharedBroker;

    /// <inheritdoc />
    public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        var broker = ResolveBroker(hostSettings);
        var infrastructure = new InMemoryTransportInfrastructure(hostSettings, receivers, this, broker);
        return Task.FromResult<TransportInfrastructure>(infrastructure);
    }

    InMemoryBroker ResolveBroker(HostSettings hostSettings) =>
        hostSettings.ServiceProvider?.GetService(typeof(InMemoryBroker)) as InMemoryBroker ?? GetBroker();

    /// <inheritdoc />
    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
    [
        TransportTransactionMode.None,
        TransportTransactionMode.ReceiveOnly,
        TransportTransactionMode.SendsAtomicWithReceive
    ];

    internal static InMemoryBroker SharedBroker { get; } = new();

    readonly InMemoryBroker? configuredBroker;
}
