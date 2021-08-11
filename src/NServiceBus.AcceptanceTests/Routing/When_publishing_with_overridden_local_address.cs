namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_publishing_with_overridden_local_address : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_delivered_to_all_subscribers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed, session => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(async (session, ctx) =>
                {
                    await session.Subscribe<MyEvent>();

                    if (ctx.HasNativePubSubSupport)
                    {
                        ctx.Subscriber1Subscribed = true;
                    }
                }))
                .Done(c => c.Subscriber1GotTheEvent)
                .Run();

            Assert.True(context.Subscriber1GotTheEvent);
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Contains("myinputqueue"))
                    {
                        context.Subscriber1Subscribed = true;
                    }
                }));
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(builder =>
                {
                    builder.DisableFeature<AutoSubscribe>();
                    builder.OverrideLocalAddress("myinputqueue");
                },
                metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            public class MyHandler : IHandleMessages<MyEvent>
            {
                public MyHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.Subscriber1GotTheEvent = true;
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