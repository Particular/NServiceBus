namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using AcceptanceTesting.Customization;
    using ScenarioDescriptors;

    public class When_registering_publishers_unobtrusive_messages_code : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_deliver_event()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<Publisher>(e => e
                    .When(c => c.Subscribed, s => s.Publish(new SomeEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.ReceivedMessage)
                .Repeat(r => r.For<AllTransportsWithMessageDrivenPubSub>())
                .Should(context =>
                {
                    Assert.That(context.Subscribed, Is.True);
                    Assert.That(context.ReceivedMessage, Is.True);
                })
                .Run();
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.Conventions().DefiningEventsAs(t => t == typeof(SomeEvent));

                    c.MessageDrivenPubSubRouting().RegisterPublisher(typeof(SomeEvent).Assembly, Conventions.EndpointNamingConvention(typeof(Publisher)));
                });
            }

            public class EventHandler : IHandleMessages<SomeEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
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