namespace NServiceBus.Transport.Msmq.AcceptanceTests.SubscriptionStorage
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;
    using Persistence.Legacy;

    public class When_using_subscription_store : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_be_delivered_to_all_subscribers()
        {
            var ctx = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, c) =>
                    {
                        c.AddTrace("Both subscribers is subscribed, going to publish MyEvent");
                        return session.Publish(new MyEvent());
                    })
                )
                .WithEndpoint<Subscriber>(b => b.When(async (session, context) =>
                {
                    await session.Subscribe<MyEvent>();
                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscribed = true;
                        context.AddTrace("Subscriber1 is now subscribed (at least we have asked the broker to be subscribed)");
                    }
                    else
                    {
                        context.AddTrace("Subscriber1 has now asked to be subscribed to MyEvent");
                    }
                }))
                .Done(c => c.GotTheEvent)
                .Run();

            Assert.IsTrue(ctx.GotTheEvent);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheEvent { get; set; }
            public bool Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        context.Subscribed = true;
                        context.AddTrace("Subscriber1 is now subscribed");
                    });
                    b.DisableFeature<AutoSubscribe>();
                    b.UsePersistence<MsmqPersistence>();
                });
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.DisableFeature<AutoSubscribe>();
                        c.UseTransport<MsmqTransport>()
                            .Routing().RegisterPublisher(typeof(MyEvent), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Publisher)));
                    }
                );
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context TestContext { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    TestContext.GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}