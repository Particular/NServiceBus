namespace NServiceBus.Transport.Msmq.AcceptanceTests
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using Features;
    using Pipeline;
    using NServiceBus.AcceptanceTests;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_with_authorizer : NServiceBusAcceptanceTest
    {

        [Test]
        public async Task Should_only_deliver_to_authorized()
        {
            var context = await Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (session, c) => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(async (session, c) =>
                {
                    await session.Subscribe<MyEvent>();
                }))
                .WithEndpoint<Subscriber2>(b => b.When(async (session, c) =>
                {
                    await session.Subscribe<MyEvent>();
                }))
                .Done(c =>
                    c.Subscriber1GotTheEvent &&
                    c.DeclinedSubscriber2)
                .Run(TimeSpan.FromSeconds(10));

            Assert.True(context.Subscriber1GotTheEvent);
            Assert.False(context.Subscriber2GotTheEvent);
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
                EndpointSetup<DefaultServer>(b =>
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

            bool Authorizer(IIncomingPhysicalMessageContext context)
            {
                var isFromSubscriber1 = context
                    .MessageHeaders["NServiceBus.SubscriberEndpoint"]
                    .EndsWith("Subscriber1");
                if (!isFromSubscriber1)
                {
                    var testContext = (TestContext)ScenarioContext;
                    testContext.DeclinedSubscriber2 = true;
                }
                return isFromSubscriber1;
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.UseTransport<MsmqTransport>()
                        .Routing().RegisterPublisher(typeof(MyEvent), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Publisher)));

                });
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
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.UseTransport<MsmqTransport>()
                        .Routing().RegisterPublisher(typeof(MyEvent), AcceptanceTesting.Customization.Conventions.EndpointNamingConvention(typeof(Publisher)));

                });
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