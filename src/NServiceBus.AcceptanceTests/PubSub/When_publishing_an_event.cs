﻿namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_be_delivered_to_allsubscribers()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.Given((bus, context) => Subscriptions.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                                context.Subscriber1Subscribed = true;

                            if (s.SubscriberReturnAddress.Queue.Contains("Subscriber2"))
                                context.Subscriber2Subscribed = true;
                        }))
                        .When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, bus => bus.Publish(new MyEvent()))
                     )
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<MyEvent>();

                            if (!Feature.IsEnabled<MessageDrivenSubscriptions>())
                                context.Subscriber1Subscribed = true;
                        }))
                      .WithEndpoint<Subscriber2>(b => b.Given((bus, context) =>
                      {
                          bus.Subscribe<MyEvent>();

                          if (!Feature.IsEnabled<MessageDrivenSubscriptions>())
                              context.Subscriber2Subscribed = true;
                      }))
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Repeat(r => r.For(Transports.Msmq))
                    .Should(c =>
                    {
                        Assert.True(c.Subscriber1GotTheEvent);
                        Assert.True(c.Subscriber2GotTheEvent);
                    })

                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }

            public bool Subscriber2GotTheEvent { get; set; }

           
            public bool Subscriber1Subscribed { get; set; }

            public bool Subscriber2Subscribed { get; set; }
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

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber1GotTheEvent = true;
                }
            }
        }

        public class Subscriber2 : EndpointConfigurationBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>(c => Configure.Features.Disable<AutoSubscribe>())
                        .AddMapping<MyEvent>(typeof(Publisher));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public void Handle(MyEvent messageThatIsEnlisted)
                {
                    Context.Subscriber2GotTheEvent = true;
                }
            }
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }
}