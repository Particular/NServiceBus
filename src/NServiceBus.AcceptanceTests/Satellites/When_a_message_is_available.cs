namespace NServiceBus.AcceptanceTests.Satellites
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Transport;

    public class When_a_message_is_available : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_the_message()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>(b => b.When((session, c) => session.Send(Endpoint.MySatelliteFeature.Address, new MyMessage())))
                .Done(c => c.MessageReceived)
                .Run();

            Assert.True(context.MessageReceived);
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
                    var instanceName = context.Settings.EndpointInstanceName();
                    var satelliteLogicalAddress = new LogicalAddress(instanceName, "MySatellite");
                    var satelliteAddress = context.Settings.GetTransportAddress(satelliteLogicalAddress);

                    context.AddSatelliteReceiver("Test satellite", satelliteAddress, TransportTransactionMode.ReceiveOnly, PushRuntimeSettings.Default,
                        (c, ec) => RecoverabilityAction.MoveToError(c.Failed.ErrorQueue),
                        (builder, pushContext) =>
                        {
                            builder.Build<Context>().MessageReceived = true;
                            return Task.FromResult(true);
                        });

                    Address = satelliteAddress;
                }

                public static string Address;
            }
        }

        class MyMessage : IMessage
        {
        }
    }
}