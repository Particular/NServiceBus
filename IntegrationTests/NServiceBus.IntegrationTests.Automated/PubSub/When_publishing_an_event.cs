namespace NServiceBus.IntegrationTests.Automated.PubSub
{
    using System;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;
    using Support;
    using Unicast;
    using Unicast.Subscriptions;

    public class When_publishing_an_event : NServiceBusIntegrationTest
    {
        [Test,Explicit("Not stable for automatic runs yet")]
        public void Should_be_delivered_to_allsubscribers()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>()
                    .WithEndpoint<Subscriber1>()
                    .WithEndpoint<Subscriber2>()
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Repeat(r => r.For<AllTransports>())
                    .Should(c =>
                    {
                    })

                    .Run();
        }

        public class Context : BehaviorContext
        {
            public bool Subscriber1GotTheEvent { get; set; }

            public bool Subscriber2GotTheEvent { get; set; }

            public int NumberOfSubscribers { get; set; }
        }
        public class Publisher : EndpointBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>()
                    .When<Context>((bus,context) =>
                        {
                            if (Configure.Instance.Configurer.HasComponent<MessageDrivenSubscriptionManager>())
                            {
                                Configure.Instance.Builder.Build<MessageDrivenSubscriptionManager>().ClientSubscribed +=
                                    (sender, args) =>
                                        {
                                            lock (context)
                                            {
                                                context.NumberOfSubscribers++;

                                                if (context.NumberOfSubscribers >= 2)
                                                    bus.Publish(new MyEvent());                
                                                
                                            }
                                        };
                            }
                            else
                            {
                                bus.Publish(new MyEvent());                
                            }
                            
                        });
            }
        }
        public class Subscriber1 : EndpointBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>()
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

        public class Subscriber2 : EndpointBuilder
        {
            public Subscriber2()
            {
                EndpointSetup<DefaultServer>()
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