namespace NServiceBus.AcceptanceTests.Msmq
{
    using System.Collections.Generic;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.PubSub;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_unsubscribing_with_authorizer : NServiceBusAcceptanceTest
    {

        [Test]
        public void Should_ignore_unsubscribe()
        {
            Scenario.Define<TestContext>()
                .WithEndpoint<Publisher>(b =>
                    b.When(c => c.Subscribed, (bus, c) =>
                    {
                        bus.Publish(new MyEvent());
                    }).When(c => c.SubscriberEventCount ==1 , (bus, c) =>
                    {
                        bus.Publish(new MyEvent());
                    })
                )
                .WithEndpoint<Subscriber>(b => b.When(c => c.PublisherStarted, bus =>
                {
                    bus.Subscribe<MyEvent>();
                }))
                .Done(c =>
                    c.SubscriberEventCount == 2 &&
                    c.DeclinedUnSubscribe)
                .Repeat(r => r.For(Transports.Msmq))
                .Run();
        }

        public class TestContext : ScenarioContext
        {
            public int SubscriberEventCount { get; set; }
            public bool UnsubscribeAttempted { get; set; }
            public bool DeclinedUnSubscribe { get; set; }
            public bool Subscribed { get; set; }
            public bool PublisherStarted { get; set; }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<TestContext>((s, context) =>
                    {
                        context.Subscribed = true;
                        context.UnsubscribeAttempted = true;
                    });
                    b.DisableFeature<AutoSubscribe>();
                });
            }

            public class CaptureStarted : IWantToRunWhenBusStartsAndStops
            {
                TestContext context;

                public CaptureStarted(TestContext context)
                {
                    this.context = context;
                }

                public void Start()
                {
                    context.PublisherStarted = true;
                }

                public void Stop()
                {
                }
            }

            public class SubscriptionAuthorizer : IAuthorizeSubscriptions
            {
                TestContext context;

                public SubscriptionAuthorizer(TestContext context)
                {
                    this.context = context;
                }

                public bool AuthorizeSubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    return true;
                }

                public bool AuthorizeUnsubscribe(string messageType, string clientEndpoint, IDictionary<string, string> headers)
                {
                    context.DeclinedUnSubscribe = true;
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
                IBus bus;
                TestContext context;

                public MyEventHandler(IBus bus, TestContext context)
                {
                    this.bus = bus;
                    this.context = context;
                }

                public void Handle(MyEvent message)
                {
                    context.SubscriberEventCount ++;
                    bus.Unsubscribe<MyEvent>();
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}