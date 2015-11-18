namespace NServiceBus.AcceptanceTests.Routing
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NServiceBus.Features;
    using NUnit.Framework;

    public class When_publishing : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Issue_1851()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher3>(b =>
                    b.When(c => c.Subscriber3Subscribed, bus => bus.Publish<IFoo>())
                    )
                .WithEndpoint<Subscriber3>(b => b.When(async (bus, context) =>
                {
                    await bus.Subscribe<IFoo>();

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
        public async Task Should_be_delivered_to_all_subscribers()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<Publisher>(b =>
                        b.When(c => c.Subscriber1Subscribed && c.Subscriber2Subscribed, (bus, c) =>
                        {
                            c.AddTrace("Both subscribers is subscribed, going to publish MyEvent");

                            var options = new PublishOptions();

                            options.SetHeader("MyHeader", "SomeValue");
                            return bus.Publish(new MyEvent(), options);
                        })
                     )
                    .WithEndpoint<Subscriber1>(b => b.When(async (bus, context) =>
                        {
                            await bus.Subscribe<MyEvent>();
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
                      .WithEndpoint<Subscriber2>(b => b.When(async (bus, context) =>
                      {
                          await bus.Subscribe<MyEvent>();

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
                        if (s.SubscriberReturnAddress.Contains("Subscriber1"))
                        {
                            context.Subscriber1Subscribed = true;
                            context.AddTrace("Subscriber1 is now subscribed");
                        }


                        if (s.SubscriberReturnAddress.Contains("Subscriber2"))
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
                    if (s.SubscriberReturnAddress.Contains("Subscriber3"))
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

                public Task Handle(IFoo messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Subscriber3GotTheEvent = true;
                    return Task.FromResult(0);
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
                public Context TestContext { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Assert.AreEqual(context.MessageHeaders["MyHeader"], "SomeValue");
                    TestContext.Subscriber1GotTheEvent = true;
                    return Task.FromResult(0);
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

                public Task Handle(MyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    Context.Subscriber2GotTheEvent = true;
                    return Task.FromResult(0);
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