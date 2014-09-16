namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishin : NServiceBusAcceptanceTest
    {
        [Test]
        public void Issue_1851()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher3>(b =>
                        b.When(c => c.Subscriber3Subscribed, bus => bus.Publish<IFoo>())
                     )
                    .WithEndpoint<Subscriber3>(b => b.Given((bus, context) =>
                    {
                        bus.Subscribe<IFoo>();

                        if (context.HasNativePubSubSupport)
                        {
                            context.Subscriber3Subscribed = true;
                        }
                    }))

                    .Done(c => c.Subscriber3GotTheEvent)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.Subscriber3GotTheEvent))
                    .Run();
        }

        [Test]
        public void Should_be_delivered_to_all_subscribers()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (bus, c) =>
                        {
                            c.AddTrace("Both subscribers is subscribed, going to publish MyEvent");
                            bus.Publish(new MyEvent());
                        })
                     )
                    .WithEndpoint<Subscriber1>(b => b.Given((bus, context) =>
                        {
                            bus.Subscribe<MyEvent>();
                            if (context.HasNativePubSubSupport)
                            {
                                context.Subscriber1Subscribed = true;
                                context.AddTrace("Subscriber1 is now subscribed (at least we have asked the broker to be subscribed)");
                            }
                            else
                            {
                                context.AddTrace("Subscriber1 has now asked to be subscribed to MyEvent");
                            }
                        }))
                      .WithEndpoint<Subscriber2>(b => b.Given((bus, context) =>
                      {
                          bus.Subscribe<MyEvent>();

                          if (context.HasNativePubSubSupport)
                          {
                              context.Subscriber2Subscribed = true;
                              context.AddTrace("Subscriber2 is now subscribed (at least we have asked the broker to be subscribed)");
                          }
                          else
                          {
                              context.AddTrace("Subscriber2 has now asked to be subscribed to MyEvent");
                          }
                      }))
                    .Done(c => c.Subscriber1GotTheEvent && c.Subscriber2GotTheEvent)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                    {
                        Assert.True(c.Subscriber1GotTheEvent);
                        Assert.True(c.Subscriber2GotTheEvent);
                    })

                    .Run(TimeSpan.FromSeconds(10));
        }

        public class Context : ScenarioContext
        {
            public bool Subscriber1GotTheEvent { get; set; }
            public bool Subscriber2GotTheEvent { get; set; }
            public bool Subscriber3GotTheEvent { get; set; }
            public bool Subscriber1Subscribed { get; set; }
            public bool Subscriber2Subscribed { get; set; }
            public bool Subscriber3Subscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Queue.Contains("Subscriber1"))
                        {
                            context.Subscriber1Subscribed = true;
                            context.AddTrace("Subscriber1 is now subscribed");
                        }


                        if (s.SubscriberReturnAddress.Queue.Contains("Subscriber2"))
                        {
                            context.AddTrace("Subscriber2 is now subscribed");
                            context.Subscriber2Subscribed = true;
                        }
                    });
                    b.DisableFeature<AutoSubscribe>();
                });
            }
        }

        public class Publisher3 : EndpointConfigurationBuilder
        {
            public Publisher3()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    if (s.SubscriberReturnAddress.Queue.Contains("Subscriber3"))
                    {
                        context.Subscriber3Subscribed = true;
                    }
                }));
            }
        }

        public class Subscriber3 : EndpointConfigurationBuilder
        {
            public Subscriber3()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<IFoo>(typeof(Publisher3));
            }

            public class MyEventHandler : IHandleMessages<IFoo>
            {
                public Context Context { get; set; }

                public void Handle(IFoo messageThatIsEnlisted)
                {
                    Context.Subscriber3GotTheEvent = true;
                }
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
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>())
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

        public interface IFoo : IEvent
        {
        }

        [Serializable]
        public class MyEvent : IEvent
        {
        }
    }
}