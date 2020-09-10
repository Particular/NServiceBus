namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NServiceBus;
    using NUnit.Framework;

    public class When_publishing_from_sendonly : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_delivered_to_all_subscribers()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<SendOnlyPublisher>(b => b.When((session, c) => session.Publish(new MyEvent())))
                .WithEndpoint<Subscriber>()
                .Done(c => c.SubscriberGotTheEvent)
                .Run();

            Assert.True(context.SubscriberGotTheEvent);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotTheEvent { get; set; }
        }

        public class SendOnlyPublisher : EndpointConfigurationBuilder
        {
            public SendOnlyPublisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.SendOnly();
                });
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.SubscriberGotTheEvent = true;

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}