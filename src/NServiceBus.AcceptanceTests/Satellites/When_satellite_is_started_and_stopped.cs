namespace NServiceBus.AcceptanceTests.Satellites
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_satellite_is_started_and_stopped : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When(async (session, c) =>
                {
                    await session.Send(Endpoint.MySatelliteFeature.Address, new MyMessage()).ConfigureAwait(false);

                    await Task.Delay(5000).ConfigureAwait(false);

                    Assert.AreEqual(1, c.MessageReceived);
                    
                    await Endpoint.MySatelliteFeature.Satellite.Stop().ConfigureAwait(false);   
                    
                    await session.Send(Endpoint.MySatelliteFeature.Address, new MyMessage()).ConfigureAwait(false);
                    
                    await Task.Delay(5000).ConfigureAwait(false);

                    Assert.AreEqual(1, c.MessageReceived);
                    
                    await Endpoint.MySatelliteFeature.Satellite.Resume().ConfigureAwait(false); 
                }))
                .Done(c => c.MessageReceived == 2)
                .Run()
                .ConfigureAwait(false);

            Assert.AreEqual(2, context.MessageReceived);
        }

        class Context : ScenarioContext
        {
            public int MessageReceived { get; set; }
            public bool TransportTransactionAddedToContext { get; set; }
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

                    Satellite = context.AddSatelliteReceiver("Test satellite", satelliteAddress, PushRuntimeSettings.Default,
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        (builder, messageContext) =>
                        {
                            var testContext = builder.Build<Context>();
                            testContext.MessageReceived++;
                            return Task.FromResult(true);
                        });

                    Address = satelliteAddress;
                }

                public static string Address;
                public static ISatellite Satellite;
            }
        }

        public class MyMessage : IMessage
        {
        }
    }
}