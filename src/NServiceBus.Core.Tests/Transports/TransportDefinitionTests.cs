namespace NServiceBus.Core.Tests.Transports;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Configuration.AdvancedExtensibility;
using NServiceBus.Features;
using NUnit.Framework;
using Transport;

[TestFixture]
public class TransportDefinitionTests
{
    [Test]
    public void Should_allow_enabling_features()
    {
        var endpointConfiguration = new EndpointConfiguration(nameof(TransportDefinitionTests));
        endpointConfiguration.UseTransport(new MyTransportDefinition());

        Assert.That(endpointConfiguration.GetSettings().IsFeatureEnabled<MyFeature>(), Is.True);
    }

    class MyTransportDefinition : TransportDefinition
    {
        public MyTransportDefinition() : base(TransportTransactionMode.None, false, false, false)
            => EnableHostFeature<MyFeature>();

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default) =>
            throw new System.NotImplementedException();

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => throw new System.NotImplementedException();
    }

    class MyFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => throw new System.NotImplementedException();
    }
}