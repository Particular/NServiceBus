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

    public class When_unsubscribing_with_authorizer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_ignore_unsubscribe()
        {
            await Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (session, c) => session.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber>(b => b.When(async (session, context) =>
                {
                    await session.Subscribe<MyEvent>();
                    await session.Unsubscribe<MyEvent>();
                }))
                .Done(c =>
                    c.SubscriberGotTheEvent &&
                    c.DeclinedUnSubscribe)
                .Run(TimeSpan.FromSeconds(10));
        }

        public class TestContext : ScenarioContext
        {
            public bool SubscriberGotTheEvent { get; set; }
            public bool UnsubscribeAttempted { get; set; }
            public bool DeclinedUnSubscribe { get; set; }
            public bool Subscribed { get; set; }
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
                        context.Subscribed = true;
                        if (s.SubscriberReturnAddress.Contains("Subscriber"))
                        {
                            context.UnsubscribeAttempted = true;
                        }
                    });
                    b.DisableFeature<AutoSubscribe>();
                });
            }

            bool Authorizer(IIncomingPhysicalMessageContext context)
            {
                var isUnsubscribe = context
                    .MessageHeaders["NServiceBus.MessageIntent"] == "Unsubscribe";
                if (!isUnsubscribe)
                {
                    return true;
                }
                var testContext = (TestContext)ScenarioContext;
                testContext.DeclinedUnSubscribe = true;
                return false;
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
                    context.SubscriberGotTheEvent = true;
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}