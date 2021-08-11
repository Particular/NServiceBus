namespace NServiceBus.AcceptanceTests.Routing.MessageDrivenSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using AcceptanceTesting.Customization;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class Sub_to_derived_event : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Base_event_should_not_be_delivered()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher>(b => b.When(c => c.SubscriberSubscribed, async session =>
                {
                    await session.Publish<IBaseEvent>();
                    await session.Send(new Done());
                }))
                .WithEndpoint<Subscriber>(b => b.When(async (session, c) => await session.Subscribe<SpecificEvent>()))
                .Done(c => c.Done)
                .Run();

            Assert.IsFalse(context.SubscriberGotEvent);
        }

        public class Context : ScenarioContext
        {
            public bool SubscriberGotEvent { get; set; }

            public bool SubscriberSubscribed { get; set; }

            public bool Done { get; set; }
        }

        public class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultPublisher>(b =>
                {
                    b.OnEndpointSubscribed<Context>((args, context) =>
                    {
                        context.SubscriberSubscribed = true;
                    });
                    b.ConfigureRouting().RouteToEndpoint(typeof(Done), typeof(Subscriber));
                });
            }
        }

        public class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                    c.LimitMessageProcessingConcurrencyTo(1); //To ensure Done is processed after the event.
                },
                metadata => metadata.RegisterPublisherFor<SpecificEvent>(typeof(Publisher)));
            }

            public class MyHandler : IHandleMessages<SpecificEvent>
            {
                public MyHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SpecificEvent messageThatIsEnlisted, IMessageHandlerContext context)
                {
                    testContext.SubscriberGotEvent = true;
                    return Task.FromResult(0);
                }

                Context testContext;
            }

            public class DoneHandler : IHandleMessages<Done>
            {
                public DoneHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(Done message, IMessageHandlerContext context)
                {
                    testContext.Done = true;
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

        public class Done : ICommand
        {
        }
    }
}