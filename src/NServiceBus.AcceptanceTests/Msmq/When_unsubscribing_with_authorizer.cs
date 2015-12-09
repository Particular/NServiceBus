namespace NServiceBus.AcceptanceTests.Msmq
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.PubSub;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_unsubscribing_with_authorizer : NServiceBusAcceptanceTest
    {
        static TestContext Context;

        [Test]
        public void Should_ignore_unsubscribe()
        {
            Context = new TestContext();
            Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (bus, c) => bus.Publish(new MyEvent()))
                )
                .WithEndpoint<Subscriber>(b => b.When(bus =>
                {
                    bus.Subscribe<MyEvent>();
                    bus.Unsubscribe<MyEvent>();
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
                    b.UseTransport<MsmqTransport>();
                    b.OnEndpointSubscribed<TestContext>((s, context) =>
                    {
                        context.Subscribed = true;
                        if (s.SubscriberReturnAddress.Queue.Contains("Subscriber"))
                        {
                            context.UnsubscribeAttempted = true;
                        }
                    });
                    b.DisableFeature<AutoSubscribe>();
                });
            }

            public class SubscriptionAuthorizer : IAuthorizeSubscriptions
            {
                public bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    return true;
                }

                public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    Context.DeclinedUnSubscribe = true;
                    return false;
                }
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

                public void Handle(MyEvent message)
                {
                    context.SubscriberGotTheEvent = true;
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}