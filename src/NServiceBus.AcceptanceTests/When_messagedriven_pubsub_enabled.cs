namespace NServiceBus.AcceptanceTests
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using MessageDrivenPubSub.Compatibility;
    using NUnit.Framework;

    public class When_messagedriven_pubsub_enabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_get_transport_definition()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<MessageDrivenPubSubEndpoint>()
                .Done(c => c.LocalEventReceived)
                .Run();

            Assert.IsTrue(context.LocalEventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EndpointSubscribed { get; set; }
            public bool LocalEventReceived { get; set; }
        }

        class MessageDrivenPubSubEndpoint : EndpointConfigurationBuilder
        {
            public MessageDrivenPubSubEndpoint() =>
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<MessageDrivenPubSubCompatibility>();
                });
        }
    }
}