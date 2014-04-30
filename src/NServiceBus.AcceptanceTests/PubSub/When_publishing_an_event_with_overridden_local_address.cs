﻿namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_event_with_overridden_local_address : NServiceBusAcceptanceTest
    {
        [Test, Explicit("This test fails against RabbitMQ")]
        public void Should_be_delivered_to_all_subscribers()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.Given((bus, context) =>
                            Subscriptions.OnEndpointSubscribed(s =>
                            {
                                if (s.SubscriberReturnAddress.Queue.Contains("myinputqueue"))
                                    context.Subscriber1Subscribed = true;
                            }))
                        .When(c => c.Subscriber1Subscribed, bus => bus.Publish(new MyEvent()))
                     )
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<MyEvent>();

                            if (!Feature.IsEnabled<MessageDrivenSubscriptions>())
                                context.Subscriber1Subscribed = true;
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
                EndpointSetup<DefaultServer>();
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<AutoSubscribe>())
                    .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class OverrideLocalAddress : IWantToRunBeforeConfiguration
            {
                public void Init()
                {
                    Address.InitializeLocalAddress("myinputqueue");
                }
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }
}