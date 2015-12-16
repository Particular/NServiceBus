namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Routing;
    using NUnit.Framework;

    public class When_distributing_an_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_round_robin()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscribersCounter == 4, async (bus, c) =>
                {
                    await bus.Publish(new MyEvent());
                }))
                .WithEndpoint<SubscriberA_1>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.IncrementSubscribersCounter();
                    }
                    return Task.FromResult(0);
                }))
                .WithEndpoint<SubscriberA_2>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.IncrementSubscribersCounter();
                    }
                    return Task.FromResult(0);
                }))
                .WithEndpoint<SubscriberB_1>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.IncrementSubscribersCounter();
                    }
                    return Task.FromResult(0);
                })).
                WithEndpoint<SubscriberB_2>(b => b.When((bus, c) =>
                {
                    if (c.HasNativePubSubSupport)
                    {
                        c.IncrementSubscribersCounter();
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
            int subscribersCounter;
            int subscriberACounter;
            int subscriberBCounter;

            public int SubscribersCounter => subscribersCounter;

            public int SubscriberACounter => subscriberACounter;

            public int SubscriberBCounter => subscriberBCounter;

            public void IncrementSubscriberACounter()
            {
                Interlocked.Increment(ref subscriberACounter);
            }
            
            public void IncrementSubscriberBCounter()
            {
                Interlocked.Increment(ref subscriberBCounter);
            }

            public void IncrementSubscribersCounter()
            {
                Interlocked.Increment(ref subscribersCounter);
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
                        context.IncrementSubscribersCounter();
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
                    c.ScaleOut().InstanceDiscriminator("1");

                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher, null, null));
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
                    c.ScaleOut().InstanceDiscriminator("2");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher, null, null));
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
                    c.ScaleOut().InstanceDiscriminator("1");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher, null, null));
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
                    c.ScaleOut().InstanceDiscriminator("2");
                    
                    var publisher = new EndpointName("DistributingAnEvent.Publisher");
                    c.Pubishers().AddStatic(publisher, typeof(MyEvent));
                    c.Routing().EndpointInstances.AddStatic(publisher, new EndpointInstance(publisher, null, null));
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
