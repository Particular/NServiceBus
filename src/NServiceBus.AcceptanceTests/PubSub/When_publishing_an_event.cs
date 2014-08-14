namespace NServiceBus.AcceptanceTests.PubSub
{
    using System;
    using System.Collections.Generic;
    using EndpointTemplates;
    using AcceptanceTesting;
    using Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_publishing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public void Issue_1851()
        {
            Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(s =>
                        {
                            if (s.SubscriberReturnAddress.Queue.Contains("Subscriber3"))
                            {
                                context.Subscriber3Subscribed = true;
                            }
                        }))
                        .When(c => c.Subscriber3Subscribed, bus => bus.Publish<IFoo>())
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
                        b.Given((bus, context) => SubscriptionBehavior.OnEndpointSubscribed(s =>
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
                                
                        }))
                        .When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (bus, c) =>
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

                    .Run();
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
                EndpointSetup<DefaultServer>(c => { }, b => b.Pipeline.Register<SubscriptionTracer.Registration>());
            }

            class SubscriptionTracer:IBehavior<OutgoingContext>
            {
                public Context Context { get; set; }
                public void Invoke(OutgoingContext context, Action next)
                {
                    next();

                    List<Address> subscribers;

                    if (context.TryGet("SubscribersForEvent",out  subscribers))
                    {
                        Context.AddTrace(string.Format("Subscribers for {0} : {1}",context.OutgoingLogicalMessage.MessageType.Name,string.Join(";",subscribers)));
                    }
                }

                public class Registration: RegisterStep
                {
                    public Registration()
                        : base("SubscriptionTracer", typeof(SubscriptionTracer), "Traces the list of found subscribers")
                    {
                        InsertBefore(WellKnownStep.DispatchMessageToTransport);
                    }
                }
            }
        }

        public class Subscriber3 : EndpointConfigurationBuilder
        {
            public Subscriber3()
            {
                EndpointSetup<DefaultServer>(_ => { }, c => c.DisableFeature<AutoSubscribe>())
                    .AddMapping<IFoo>(typeof(Publisher));
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
                EndpointSetup<DefaultServer>(_ => { }, c => c.DisableFeature<AutoSubscribe>())
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
                EndpointSetup<DefaultServer>(_ => { }, c => c.DisableFeature<AutoSubscribe>())
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