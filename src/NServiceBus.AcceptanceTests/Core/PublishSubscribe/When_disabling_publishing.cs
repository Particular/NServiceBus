namespace NServiceBus.AcceptanceTests.PublishSubscribe
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Configuration.AdvancedExtensibility;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_disabling_publishing : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_subscribe_to_and_receive_events()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithDisabledPublishing>(e => e.When(
                    c => c.Subscribe<TestEvent>()))
                .WithEndpoint<MessageDrivenPublisher>(e => e.When(c =>
                {
                    if (c.ReceivedSubscription)
                    {
                        return Task.FromResult(true);
                    }

                    return Task.FromResult(false);
                }, s => s.Publish(new TestEvent())))
                .Done(c => c.ReceivedEvent)
                .Run();
        }

        [Test]
        public void Should_throw_when_publishing()
        {
            var exception = Assert.ThrowsAsync<InvalidOperationException>(() => Scenario.Define<Context>()
                .WithEndpoint<EndpointWithDisabledPublishing>(e => e.When(
                    c => c.Publish(new TestEvent())))
                .Done(c => c.EndpointsStarted)
                .Run());

            StringAssert.Contains("Publishing has been explicitly disabled on this endpoint", exception.Message);
        }

        class Context : ScenarioContext
        {
            public bool ReceivedSubscription { get; set; }
            public bool ReceivedEvent { get; set; }
        }

        class EndpointWithDisabledPublishing : EndpointConfigurationBuilder
        {
            public EndpointWithDisabledPublishing()
            {
                var template = new DefaultServer();
                template.TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true);
                EndpointSetup(template,
                    // DisablePublishing API is only available on the message-driven pub/sub transport settings.
                    (c, _) => c.GetSettings().Set("NServiceBus.PublishSubscribe.EnablePublishing", false),
                    pm => pm.RegisterPublisherFor<TestEvent>(typeof(MessageDrivenPublisher)));
            }

            class EventHandler : IHandleMessages<TestEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(TestEvent message, IMessageHandlerContext context)
                {
                    testContext.ReceivedEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        class MessageDrivenPublisher : EndpointConfigurationBuilder
        {
            public MessageDrivenPublisher()
            {
                var template = new DefaultServer
                {
                    TransportConfiguration = new ConfigureEndpointAcceptanceTestingTransport(false, true),
                    PersistenceConfiguration = new ConfigureEndpointInMemoryPersistence()
                };

                EndpointSetup(template, (c, _) => c.OnEndpointSubscribed<Context>((args, context) =>
                {
                    if (args.MessageType.Contains(typeof(TestEvent).FullName))
                    {
                        context.ReceivedSubscription = true;
                    }
                }));
            }
        }

        public class TestEvent : IEvent
        {
        }
    }
}