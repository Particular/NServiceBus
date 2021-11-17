namespace NServiceBus.AcceptanceTests.Core.TransportSeam
{
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
            Assert.AreEqual("SomeAddress", context.ResolvedAddress);
            Assert.AreEqual(endpointName, context.ReceiveAddresses.MainReceiveAddress);
            Assert.AreEqual(endpointName + "-MyInstance", context.ReceiveAddresses.InstanceReceiveAddress);
            Assert.AreEqual("MySatellite", context.ReceiveAddresses.SatelliteReceiveAddresses.Single());
        }

        class Context : ScenarioContext
        {
            public ReceiveAddresses TransportDefinition { get; set; }
            public string ResolvedAddress { get; set; }
            public ReceiveAddresses ReceiveAddresses { get; set; }
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
}