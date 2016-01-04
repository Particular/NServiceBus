namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_unsubscribing_with_authorizer : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_ignore_unsubscribe()
        {
            await Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (bus, c) => bus.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber>(b => b.When(async (bus, context) =>
                {
                    await bus.Subscribe<MyEvent>();
                    await bus.Unsubscribe<MyEvent>();
                }))
                .Done(c =>
                    c.SubscriberGotTheEvent &&
                    c.DeclinedUnSubscribe)
                .Repeat(r => r.For(Transports.Msmq))
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