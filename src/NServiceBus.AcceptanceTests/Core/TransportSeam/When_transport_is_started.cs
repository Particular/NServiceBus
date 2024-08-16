namespace NServiceBus.AcceptanceTests.Core.TransportSeam;

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Features;
using NServiceBus.Transport;
using NUnit.Framework;

public class When_transport_is_started : NServiceBusAcceptanceTest
{
    [Test]
    public async Task Should_provide_access_to_addresses_and_address_resolution()
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>()
            .Done(c => c.EndpointsStarted)
            .Run();

        var endpointName = AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Endpoint));

        Assert.That(context.ResolvedAddress, Is.EqualTo("SomeAddress"));
        Assert.That(context.ReceiveAddresses.MainReceiveAddress, Is.EqualTo(endpointName));
        Assert.That(context.ReceiveAddresses.InstanceReceiveAddress, Is.EqualTo(endpointName + "-MyInstance"));
        Assert.That(context.ReceiveAddresses.SatelliteReceiveAddresses.Single(), Is.EqualTo("MySatellite"));
        Assert.That(context.LocalQueueAddress.ToString(), Is.EqualTo(endpointName));
        Assert.That(context.InstanceSpecificQueueAddress.ToString(), Is.EqualTo(endpointName + "-MyInstance"));
    }

    class Context : ScenarioContext
    {
        public string ResolvedAddress { get; set; }
        public ReceiveAddresses ReceiveAddresses { get; set; }
        public QueueAddress LocalQueueAddress { get; set; }
        public QueueAddress InstanceSpecificQueueAddress { get; set; }
    }

    class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint()
        {
            EndpointSetup<DefaultServer>(c =>
            {
                c.EnableFeature<FeatureAccessingAddressing>();
                c.MakeInstanceUniquelyAddressable("MyInstance");
            });
        }

        class FeatureAccessingAddressing : Feature
        {
            protected override void Setup(FeatureConfigurationContext context)
            {
                var testContext = (Context)context.Settings.Get<ScenarioContext>();

                testContext.LocalQueueAddress = context.LocalQueueAddress();
                testContext.InstanceSpecificQueueAddress = context.InstanceSpecificQueueAddress();

                context.AddSatelliteReceiver("Test satellite",
                        new QueueAddress("MySatellite"), PushRuntimeSettings.Default,
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        (builder, messageContext, cancellationToken) => Task.FromResult(true));

                context.RegisterStartupTask(s => new StartupTask(testContext,
                    (ITransportAddressResolver)s.GetService(typeof(ITransportAddressResolver)),
                    (ReceiveAddresses)s.GetService(typeof(ReceiveAddresses))));
            }
        }

        class StartupTask : FeatureStartupTask
        {
            readonly Context testContext;
            readonly ITransportAddressResolver resolver;
            readonly ReceiveAddresses receiveAddresses;

            public StartupTask(Context testContext, ITransportAddressResolver resolver, ReceiveAddresses receiveAddresses)
            {
                this.testContext = testContext;
                this.resolver = resolver;
                this.receiveAddresses = receiveAddresses;
            }
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
            {
                testContext.ResolvedAddress = resolver.ToTransportAddress(new QueueAddress("SomeAddress"));
                testContext.ReceiveAddresses = receiveAddresses;
                return Task.CompletedTask;
            }

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default)
            {
                return Task.CompletedTask;
            }
        }
    }
}