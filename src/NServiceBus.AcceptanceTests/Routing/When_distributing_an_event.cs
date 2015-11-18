namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NUnit.Framework;

    public class When_distributing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberA_1Subscribed && c.SubscriberA_2Subscribed && c.SubscriberB_1Subscribed && c.SubscriberB_2Subscribed, async (bus, c) =>
                {
                    await bus.Publish(new MyEvent());
                }))
                .WithEndpoint<SubscriberA_1>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.SubscriberA_1Subscribed = true;
                    }
                    return Task.FromResult(0);
                }))
                .WithEndpoint<SubscriberA_2>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.SubscriberA_2Subscribed = true;
                    }
                    return Task.FromResult(0);
                }))
                .WithEndpoint<SubscriberB_1>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.SubscriberB_1Subscribed = true;
                    }
                    return Task.FromResult(0);
                })).
                WithEndpoint<SubscriberB_2>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.SubscriberB_2Subscribed = true;
                    }
                    return Task.FromResult(0);
                }))
                .Done(c => c.SubscriberACounter > 0 && c.SubscriberBCounter > 0)
                .Run();

            Assert.IsTrue(context.SubscriberACounter == 1);
            Assert.IsTrue(context.SubscriberBCounter == 1);
        }

        public class Context : ScenarioContext
        {
            int subscriberACounter;
            int subscriberBCounter;

            public int SubscriberACounter => subscriberACounter;

            public int SubscriberBCounter => subscriberBCounter;

            public bool SubscriberA_1Subscribed { get; set; }
            public bool SubscriberA_2Subscribed { get; set; }
            public bool SubscriberB_1Subscribed { get; set; }
            public bool SubscriberB_2Subscribed { get; set; }

            public void IncrementSubscriberACounter()
            {
                Interlocked.Increment(ref subscriberACounter);
            }
            
            public void IncrementSubscriberBCounter()
            {
                Interlocked.Increment(ref subscriberBCounter);
            }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.OnEndpointSubscribed<Context>((s, context) =>
                    {
                        if (s.SubscriberReturnAddress.Contains("SubscriberA-1"))
                        {
                            context.SubscriberA_1Subscribed = true;
                        }
                        if (s.SubscriberReturnAddress.Contains("SubscriberA-2"))
                        {
                            context.SubscriberA_2Subscribed = true;
                        }
                        if (s.SubscriberReturnAddress.Contains("SubscriberB-1"))
                        {
                            context.SubscriberB_1Subscribed = true;
                        }
                        if (s.SubscriberReturnAddress.Contains("SubscriberB-2"))
                        {
                            context.SubscriberB_2Subscribed = true;
                        }
                    });
                });
            }
        }

        public class SubscriberA_1 : EndpointConfigurationBuilder
        {
            public SubscriberA_1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingAnEvent.SubscriberA");
                    c.ScaleOut().UniqueQueuePerEndpointInstance("1");

                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstanceName(publisher, null, null));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementSubscriberACounter();
                    return Task.FromResult(0);
                }
            }
        }
        
        public class SubscriberA_2 : EndpointConfigurationBuilder
        {
            public SubscriberA_2()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingAnEvent.SubscriberA");
                    c.ScaleOut().UniqueQueuePerEndpointInstance("2");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstanceName(publisher, null, null));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementSubscriberACounter();
                    return Task.FromResult(0);
                }
            }
        }

        public class SubscriberB_1 : EndpointConfigurationBuilder
        {
            public SubscriberB_1()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingAnEvent.SubscriberB");
                    c.ScaleOut().UniqueQueuePerEndpointInstance("1");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstanceName(publisher, null, null));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }
                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementSubscriberBCounter();
                    return Task.FromResult(0);
                }
            }
        }
        
        public class SubscriberB_2 : EndpointConfigurationBuilder
        {
            public SubscriberB_2()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EndpointName("DistributingAnEvent.SubscriberB");
                    c.ScaleOut().UniqueQueuePerEndpointInstance("2");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstanceName(publisher, null, null));
                });
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public Context Context { get; set; }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    Context.IncrementSubscriberBCounter();
                    return Task.FromResult(0);
                }
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}
