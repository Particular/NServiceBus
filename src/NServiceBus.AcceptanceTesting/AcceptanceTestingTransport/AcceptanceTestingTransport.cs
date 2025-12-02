#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Routing;
using Transport;

public class AcceptanceTestingTransport(
    bool enableNativeDelayedDelivery = true,
    bool enableNativePublishSubscribe = true)
    : TransportDefinition(TransportTransactionMode.SendsAtomicWithReceive, enableNativeDelayedDelivery,
        enableNativePublishSubscribe, true), IMessageDrivenSubscriptionTransport
{
    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostSettings);

        var infrastructure = new AcceptanceTestingTransportInfrastructure(hostSettings, this, receivers);
        infrastructure.ConfigureDispatcher();
        await infrastructure.ConfigureReceivers().ConfigureAwait(false);

        return infrastructure;
    }

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() =>
    [
        TransportTransactionMode.None,
        TransportTransactionMode.ReceiveOnly,
        TransportTransactionMode.SendsAtomicWithReceive
    ];

    public string? StorageLocation
    {
        get;
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            PathChecker.ThrowForBadPath(value, nameof(StorageLocation));
            field = value;
        }
    }
}