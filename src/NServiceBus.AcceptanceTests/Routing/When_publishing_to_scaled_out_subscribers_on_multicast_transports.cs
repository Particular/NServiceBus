namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.AcceptanceTests.ScenarioDescriptors;
    using NUnit.Framework;

    public class When_publishing_to_scaled_out_subscribers_on_multicast_transports : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Each_event_should_be_delivered_to_single_instance_of_each_subscriber()
        {
            await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.EndpointsStarted, async (session, c) => { await session.Publish(new MyEvent()); }))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("1")))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("2")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("1")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.ScaleOut().InstanceDiscriminator("2")))
                .Done(c => c.SubscriberACounter > 0 && c.SubscriberBCounter > 0)
                .Repeat(r => r.For<AllTransportsWithCentralizedPubSubSupport>())
                .Should(c =>
                {
                    Assert.IsTrue(c.SubscriberACounter == 1);
                    Assert.IsTrue(c.SubscriberBCounter == 1);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            int subscriberACounter;
            int subscriberBCounter;

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
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>();
            }
        }

        public class SubscriberA : EndpointConfigurationBuilder
        {
            public SubscriberA()
            {
                EndpointSetup<DefaultServer>();
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

        public class SubscriberB : EndpointConfigurationBuilder
        {
            public SubscriberB()
            {
                EndpointSetup<DefaultServer>();
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