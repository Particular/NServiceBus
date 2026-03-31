namespace NServiceBus;

using System;
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
    /// Creates a new instance of the in-memory transport.
    /// </summary>
    /// <param name="broker">
    /// Optional broker to use when dependency injection does not provide an <see cref="InMemoryBroker" />.
    /// </param>
    /// <remarks>
    /// When multiple endpoints need to communicate in-memory, they should share the same broker instance.
    /// Broker resolution is optional and uses the following precedence: an <see cref="InMemoryBroker" /> resolved from
    /// <see cref="HostSettings.ServiceProvider" />, then the broker provided to this constructor, and finally the shared broker.
    /// For testing, omit the broker parameter and the shared broker will be used unless dependency injection supplies one.
    /// </remarks>
    public InMemoryTransport(InMemoryBroker? broker = null)
        : base(TransportTransactionMode.SendsAtomicWithReceive, supportsDelayedDelivery: true, supportsPublishSubscribe: true, supportsTTBR: true)
    {
        configuredBroker = broker;
    }

    /// <summary>
    /// Creates a new instance of the in-memory transport with inline execution enabled.
    /// </summary>
    /// <param name="broker">
    /// Optional broker to use when dependency injection does not provide an <see cref="InMemoryBroker" />.
    /// </param>
    /// <param name="inlineExecutionOptions">Inline execution options to snapshot for this transport instance.</param>
    public InMemoryTransport(InMemoryBroker? broker, InlineExecutionOptions inlineExecutionOptions)
        : base(TransportTransactionMode.SendsAtomicWithReceive, supportsDelayedDelivery: true, supportsPublishSubscribe: true, supportsTTBR: true)
    {
        ArgumentNullException.ThrowIfNull(inlineExecutionOptions);

        configuredBroker = broker;
        InlineExecutionSettings = new InlineExecutionSettings(inlineExecutionOptions);

        // Enable the feature that will register the recoverability behavior
        EnableEndpointFeature<InlineExecutionFeature>();
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

    internal InlineExecutionSettings InlineExecutionSettings { get; } = InlineExecutionSettings.Disabled;
}
