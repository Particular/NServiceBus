namespace NServiceBus.AcceptanceTests.PublishSubscribe
{
    using System;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_unsubscribing_on_send_only_endpoint : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_throw_InvalidOperationException_on_native_pubsub()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<NativePubSubSendOnlyEndpoint>(e => e
                    .When(s => s.Unsubscribe<SomeEvent>()))
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Send-only endpoints cannot unsubscribe to events", exception.Message);
        }

        [Test]
        public void Should_throw_InvalidOperationException_on_message_driven_pubsub()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<ScenarioContext>()
                .WithEndpoint<MessageDrivenPubSubSendOnlyEndpoint>(e => e
                    .When(s => s.Unsubscribe<SomeEvent>()))
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Send-only endpoints cannot unsubscribe to events", exception.Message);
        }

        class NativePubSubSendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public NativePubSubSendOnlyEndpoint()
            {
                var template = new DefaultServer
                {
                    TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(true, true)
                };

                EndpointSetup(template, (configuration, _) => configuration.SendOnly());
            }
        }

        class MessageDrivenPubSubSendOnlyEndpoint : EndpointConfigurationBuilder
        {
            public MessageDrivenPubSubSendOnlyEndpoint()
            {
                var template = new DefaultServer
                {
                    TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true)
                };

                EndpointSetup(template, (configuration, _) => configuration.SendOnly());
            }
        }

        public class SomeEvent : IEvent
        {
        }
    }
}