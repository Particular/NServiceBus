namespace NServiceBus.AcceptanceTests.Satellites
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_satellite_txmode_does_not_match_endpoints_txmode : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw()
        {
            var exception = Assert.ThrowsAsync<Exception>(async () => await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Send(Endpoint.MySatelliteFeature.Address, new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run());

            Assert.That(exception.Message, Does.Contain("AddSatelliteReceiver").And.Contain($"{nameof(TransportTransactionMode.None)}"));
        }

        class Context : ScenarioContext
        {
            public bool MessageReceived { get; set; }
        }

        class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MySatelliteFeature : Feature
            {
                public MySatelliteFeature()
                {
                    EnableByDefault();
                }

                protected override void Setup(FeatureConfigurationContext context)
                {
                    var satelliteLogicalAddress = context.Settings.LogicalAddress().CreateQualifiedAddress("MySatellite");
                    var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

#pragma warning disable 612, 618
                    context.AddSatelliteReceiver("Test satellite", satelliteAddress, TransportTransactionMode.None, PushRuntimeSettings.Default,
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        (builder, pushContext) =>
                        {
                            var testContext = builder.Build<Context>();
                            testContext.MessageReceived = true;
                            return Task.FromResult(true);
                        });
#pragma warning restore 612, 618

                    Address = satelliteAddress;
                }

                public static string Address;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}