namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_registering_publishers_unobtrusive_messages_code : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_deliver_event()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(e => e
                    .When(c => c.Subscribed, s => s.Publish(new SomeEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.ReceivedMessage)
                .Run();

            Assert.That(context.Subscribed, Is.True);
            Assert.That(context.ReceivedMessage, Is.True);
        }

        public class Context : ScenarioContext
        {
            public bool Subscribed { get; set; }
            public bool ReceivedMessage { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((e, ctx) => ctx.Subscribed = true);
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));
                }).ExcludeType<SomeEvent>();
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(
                    c => c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent)),
                    metadata => metadata.RegisterPublisherFor<SomeEvent>(typeof(Publisher)));
            }

            public class Handler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public Handler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(SomeEvent message, IMessageHandlerContext context)
                {
                    testContext.ReceivedMessage = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class SomeEvent
        {
        }
    }
}