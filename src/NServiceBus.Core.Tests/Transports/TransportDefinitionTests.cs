namespace NServiceBus.Core.Tests.Transports;

using System.Collections.Frozen;
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
            => EnableEndpointFeature<MyFeature>();

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default) =>
            throw new System.NotImplementedException();

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes() => throw new System.NotImplementedException();
    }

    class MyFeature : Feature
    {
        protected override void Setup(FeatureConfigurationContext context) => throw new System.NotImplementedException();
    }

    [Test]
    public void DispatchPropertyNamesToPreserve_should_default_to_empty()
    {
        var definition = new TestTransportDefinition();
        Assert.That(definition.GetDispatchPropertyNamesToPreserve(), Is.Empty);
    }

    [Test]
    public void DispatchPropertyNamesToPreserve_can_be_overridden()
    {
        var definition = new TransportWithPreservation();
        Assert.Multiple(() =>
        {
            Assert.That(definition.GetDispatchPropertyNamesToPreserve(), Contains.Item("AWS.SQS.MessageGroupId"));
            Assert.That(definition.GetDispatchPropertyNamesToPreserve(), Contains.Item("AWS.SQS.MessageDeduplicationId"));
        });
    }

    class TestTransportDefinition : TransportDefinition
    {
        public TestTransportDefinition() : base(TransportTransactionMode.None, false, false, false) { }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default) =>
            throw new System.NotImplementedException();

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
            => [TransportTransactionMode.None];

        public FrozenSet<string> GetDispatchPropertyNamesToPreserve() => DispatchPropertyNamesToPreserve;
    }

    class TransportWithPreservation : TransportDefinition
    {
        public TransportWithPreservation() : base(TransportTransactionMode.None, false, false, false) { }

        public override Task<TransportInfrastructure> Initialize(HostSettings hostSettings, ReceiveSettings[] receivers, string[] sendingAddresses,
            CancellationToken cancellationToken = default) =>
            throw new System.NotImplementedException();

        public override IReadOnlyCollection<TransportTransactionMode> GetSupportedTransactionModes()
            => [TransportTransactionMode.None];

        protected internal override FrozenSet<string> DispatchPropertyNamesToPreserve { get; }
            = FrozenSet.ToFrozenSet(["AWS.SQS.MessageGroupId", "AWS.SQS.MessageDeduplicationId"]);

        public FrozenSet<string> GetDispatchPropertyNamesToPreserve() => DispatchPropertyNamesToPreserve;
    }
}