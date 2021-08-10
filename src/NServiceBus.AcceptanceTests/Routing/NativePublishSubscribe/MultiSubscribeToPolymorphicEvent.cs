namespace NServiceBus.AcceptanceTests.Routing.NativePublishSubscribe
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class MultiSubscribeToPolymorphicEvent : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Both_events_should_be_delivered()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher1>(b => b.When(c => c.EndpointsStarted, (session, c) =>
                {
                    c.AddTrace("Publishing MyEvent1");
                    return session.Publish(new MyEvent1());
                }))
                .WithEndpoint<Publisher2>(b => b.When(c => c.EndpointsStarted, (session, c) =>
                {
                    c.AddTrace("Publishing MyEvent2");
                    return session.Publish(new MyEvent2());
                }))
                .WithEndpoint<Subscriber>()
                .Done(c => c.SubscriberGotIMyEvent && c.SubscriberGotMyEvent2)
                .Run();

            Assert.True(context.SubscriberGotIMyEvent);
            Assert.True(context.SubscriberGotMyEvent2);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotIMyEvent { get; set; }
            public bool SubscriberGotMyEvent2 { get; set; }
        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultPublisher>();
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultPublisher>();
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>();
            }

            public class MyHandler : IHandleMessages<IMyEvent>
            {
                public MyHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(IMyEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.AddTrace($"Got event '{messageThatIsEnlisted}'");
                    if (messageThatIsEnlisted is MyEvent2)
                    {
                        testContext.SubscriberGotMyEvent2 = true;
                    }
                    else
                    {
                        testContext.SubscriberGotIMyEvent = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class MyEvent1 : IMyEvent
        {
        }

        public class MyEvent2 : IMyEvent
        {
        }

        public interface IMyEvent : IEvent
        {
        }
    }
}