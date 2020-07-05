namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_subscribing_to_a_base_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Both_base_and_specific_events_should_be_delivered()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
                {
                    await session.Publish(new SpecificEvent());
                    await session.Publish<IBaseEvent>();
                }))
                .WithEndpoint<GeneralSubscriber>(b => b.When(async (session, c) => await session.Subscribe<IBaseEvent>()))
                .Done(c => c.SubscriberGotBaseEvent && c.SubscriberGotSpecificEvent)
                .Run();

            Assert.True(context.SubscriberGotBaseEvent);
            Assert.True(context.SubscriberGotSpecificEvent);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotBaseEvent { get; set; }
            public bool SubscriberGotSpecificEvent { get; set; }
            public bool SubscriberSubscribed { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((args, context) => { context.SubscriberSubscribed = true; }));
            }
        }

        public class GeneralSubscriber : EndpointConfigurationBuilder
        {
            public GeneralSubscriber()
            {
                EndpointSetup<DefaultServer>(c => { c.DisableFeature<AutoSubscribe>(); },
                    metadata => metadata.RegisterPublisherFor<IBaseEvent>(typeof(Publisher)));
            }

            public class MyEventHandler : IHandleMessages<IBaseEvent>
            {
                public MyEventHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(IBaseEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    if (messageThatIsEnlisted is SpecificEvent)
                    {
                        testContext.SubscriberGotSpecificEvent = true;
                    }
                    else
                    {
                        testContext.SubscriberGotBaseEvent = true;
                    }
                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }

        public class SpecificEvent : IBaseEvent
        {
        }

        public interface IBaseEvent : IEvent
        {
        }
    }
}