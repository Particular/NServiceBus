namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using Features;
using Routing;
using Settings;
using Transport;

public class AcceptanceTestingTransport : TransportDefinition, IMessageDrivenSubscriptionTransport
{
    public AcceptanceTestingTransport(bool enableNativeDelayedDelivery = true, bool enableNativePublishSubscribe = true)
        : base(TransportTransactionMode.SendsAtomicWithReceive, enableNativeDelayedDelivery, enableNativePublishSubscribe, true)
    {
    }

    public override void ConfigureForEndpointHosting(SettingsHolder settings) => settings.EnableFeature<MyFeature>();

    public override async Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(hostSettings);
        
        var infrastructure = new AcceptanceTestingTransportInfrastructure(hostSettings, this, receivers);
        infrastructure.ConfigureDispatcher();
        await infrastructure.ConfigureReceivers().ConfigureAwait(false);

        return infrastructure;
    }

    public class MyFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => Console.WriteLine("Hello world");
    }

    public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
    {
        return new[]
        {
            TransportTransactionMode.None, TransportTransactionMode.ReceiveOnly, TransportTransactionMode.SendsAtomicWithReceive
        };
    }

    public string StorageLocation
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