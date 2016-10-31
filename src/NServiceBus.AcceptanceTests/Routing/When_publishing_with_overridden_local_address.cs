namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_with_overridden_local_address : NServiceBusAcceptanceTest
    {
        [Test, Explicit("This test fails against RabbitMQ")]
        public async Task Should_be_delivered_to_all_subscribers()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed, session => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(async (session, context) =>
                {
                    await session.Subscribe<MyEvent>();

                    if (context.HasNativePubSubSupport)
                    {
                        context.Subscriber1Subscribed = true;
                    }
                }))
                .Done(c => c.Subscriber1GotTheEvent)
                .Repeat(r => r.For(Transports.Default))
                .Should(c => Assert.True(c.Subscriber1GotTheEvent))
                .Run();
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
                },
                metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Subscriber1GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }


        public class MyEvent : IEvent
        {
        }
    }
}