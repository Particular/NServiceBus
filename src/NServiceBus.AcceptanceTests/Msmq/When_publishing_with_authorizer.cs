namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing_with_authorizer : NServiceBusAcceptanceTest
    {
        static TestContext Context;

        [Test]
        public async Task Should_only_deliver_to_authorized()
        {
            await Scenario.Define<TestContext>(context => Context = context)
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (bus, c) => bus.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(async (bus, context) =>
                {
                    await bus.Subscribe<MyEvent>();
                }))
                .WithEndpoint<Subscriber2>(b => b.When(async (bus, context) =>
                {
                    await bus.Subscribe<MyEvent>();
                }))
                .Done(c =>
                    c.Subscriber1GotTheEvent && 
                    c.DeclinedSubscriber2)
                .Repeat(r => r.For(Transports.Msmq))
                .Should(c =>
                {
                    Assert.True(c.Subscriber1GotTheEvent);
                    Assert.False(c.Subscriber2GotTheEvent);
                })
                .Run(TimeSpan.FromSeconds(10));
        }

        public class TestContext : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber2GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
            public bool Subscriber2Subscribed { get; set; }
            public bool DeclinedSubscriber2 { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.UseTransport<MsmqTransport>().SubscriptionAuthorizer(Authorizer);
                    b.OnEndpointSubscribed<TestContext>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Contains("Subscriber1"))
                        {
                            context.Subscriber1Subscribed = true;
                        }

                        if (s.SubscriberReturnAddress.Contains("Subscriber2"))
                        {
                            context.Subscriber2Subscribed = true;
                        }
                    });
                    b.DisableFeature<AutoSubscribe>();
                });
            }

            bool Authorizer(IncomingPhysicalMessageContext context)
            {
                var isFromSubscriber1 = context
                    .MessageHeaders["NServiceBus.SubscriberEndpoint"]
                    .EndsWith("Subscriber1");
                if (!isFromSubscriber1)
                {
                    Context.DeclinedSubscriber2 = true;
                }
                return isFromSubscriber1;
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                TestContext context;

                public MyEventHandler(TestContext context)
                {
                    this.context = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext handlerContext)
                {
                    context.Subscriber1GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                TestContext context;

                public MyEventHandler(TestContext context)
                {
                    this.context = context;
                }

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext handlerContext)
                {
                    context.Subscriber2GotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}