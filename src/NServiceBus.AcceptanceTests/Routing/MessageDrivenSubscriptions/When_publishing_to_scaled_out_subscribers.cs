namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_publishing_to_scaled_out_subscribers : NServiceBusAcceptanceTest
    {
       [Test]
        public async Task Each_event_should_be_delivered_to_single_instance_of_each_subscriber()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscribersCounter == 4, async (session, c) => { await session.Publish(new MyEvent()); }))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<SubscriberA>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("1")))
                .WithEndpoint<SubscriberB>(b => b.CustomConfig(c => c.MakeInstanceUniquelyAddressable("2")))
                .Done(c => c.ProcessedByA > 0 && c.ProcessedByB > 0)
                .Run();

            Assert.AreEqual(1, context.ProcessedByA);
            Assert.AreEqual(1, context.ProcessedByB);

        }

        public class Context : ScenarioContext
        {
            public int SubscribersCounter => subscribersCounter;

            public int ProcessedByA => processedByA;

            public int ProcessedByB => processedByB;

            public void IncrementA()
            {
                Interlocked.Increment(ref processedByA);
            }

            public void IncrementB()
            {
                Interlocked.Increment(ref processedByB);
            }

            public void IncrementSubscribersCounter()
            {
                Interlocked.Increment(ref subscribersCounter);
            }

            int processedByA;
            int processedByB;
            int subscribersCounter;
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c => { c.OnEndpointSubscribed<Context>((s, context) => { context.IncrementSubscribersCounter(); }); });
            }
        }

        public class SubscriberA : EndpointConfigurationBuilder
        {
            public SubscriberA()
            {
                EndpointSetup<DefaultServer>(publisherMetadata: metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.IncrementA();
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class SubscriberB : EndpointConfigurationBuilder
        {
            public SubscriberB()
            {
                EndpointSetup<DefaultServer>(publisherMetadata: metadata => metadata.RegisterPublisherFor<MyEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<MyEvent>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(MyEvent message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.IncrementB();
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