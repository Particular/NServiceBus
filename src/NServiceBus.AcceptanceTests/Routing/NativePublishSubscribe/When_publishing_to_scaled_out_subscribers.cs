namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_to_scaled_out_subscribers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_route_event_to_shared_queue()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b
                    .When(c => c.EndpointsStarted, (session, c) => session.Publish(new MyEvent())))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .Done(c => c.SubscriberACounter > 0 && c.SubscriberBCounter > 0)
                .Run();

            Assert.AreEqual(1, context.SubscriberACounter);
            Assert.AreEqual(1, context.SubscriberBCounter);
        }

        public class Context : ScenarioContext
        {
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

            int subscriberACounter;
            int subscriberBCounter;
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
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.IncrementSubscriberACounter();
                    return Task.FromResult(0);
                }

                Context testContext;
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
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context)
                {
                    testContext.IncrementSubscriberBCounter();
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyEvent : IEvent
        {
        }
    }
}