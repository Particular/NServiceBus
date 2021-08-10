namespace NServiceBus.AcceptanceTests.Routing
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;
    using Conventions = AcceptanceTesting.Customization.Conventions;

    public class When_base_event_from_2_publishers : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_events_from_all_publishers()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Publisher1>(b =>
                    b.When(c => c.SubscribedToPublisher1, session => session.Publish(new DerivedEvent1()))
                )
                .WithEndpoint<Publisher2>(b =>
                    b.When(c => c.SubscribedToPublisher2, session => session.Publish(new DerivedEvent2()))
                )
                .WithEndpoint<Subscriber1>(b => b.When(c => c.EndpointsStarted, async (session, c) =>
                {
                    await session.Subscribe<DerivedEvent1>();
                    await session.Subscribe<DerivedEvent2>();

                    if (c.HasNativePubSubSupport)
                    {
                        c.SubscribedToPublisher1 = true;
                        c.SubscribedToPublisher2 = true;
                    }
                }))
                .Done(c => c.GotTheEventFromPublisher1 && c.GotTheEventFromPublisher2)
                .Run();

            Assert.True(context.GotTheEventFromPublisher1);
            Assert.True(context.GotTheEventFromPublisher2);
        }

        public class Context : ScenarioContext
        {
            public bool GotTheEventFromPublisher1 { get; set; }
            public bool GotTheEventFromPublisher2 { get; set; }
            public bool SubscribedToPublisher1 { get; set; }
            public bool SubscribedToPublisher2 { get; set; }
        }

        public class Publisher1 : EndpointConfigurationBuilder
        {
            public Publisher1()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.AddTrace($"{Conventions.EndpointNamingConvention(typeof(Publisher1))} SubscriberEndpoint={s.SubscriberEndpoint}");
                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber1))))
                    {
                        context.SubscribedToPublisher1 = true;
                    }
                }));
            }
        }

        public class Publisher2 : EndpointConfigurationBuilder
        {
            public Publisher2()
            {
                EndpointSetup<DefaultPublisher>(b => b.OnEndpointSubscribed<Context>((s, context) =>
                {
                    context.AddTrace($"{Conventions.EndpointNamingConvention(typeof(Publisher2))} SubscriberEndpoint={s.SubscriberEndpoint}");

                    if (s.SubscriberEndpoint.Contains(Conventions.EndpointNamingConvention(typeof(Subscriber1))))
                    {
                        context.SubscribedToPublisher2 = true;
                    }
                }));
            }
        }

        public class Subscriber1 : EndpointConfigurationBuilder
        {
            public Subscriber1()
            {
                EndpointSetup<DefaultServer>(c => c.DisableFeature<AutoSubscribe>(),
                    metadata =>
                    {
                        metadata.RegisterPublisherFor<DerivedEvent1>(typeof(Publisher1));
                        metadata.RegisterPublisherFor<DerivedEvent2>(typeof(Publisher2));
                    });
            }

            public class Handler : IHandleMessages<BaseEvent>
            {
                public Handler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(BaseEvent message, IMessageHandlerContext context)
                {
                    if (message.GetType().FullName.Contains(nameof(DerivedEvent1)))
                    {
                        testContext.GotTheEventFromPublisher1 = true;
                    }
                    if (message.GetType().FullName.Contains(nameof(DerivedEvent1)))
                    {
                        testContext.GotTheEventFromPublisher2 = true;
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
            }
        }


        public class BaseEvent : IEvent
        {
        }


        public class DerivedEvent1 : BaseEvent
        {
        }


        public class DerivedEvent2 : BaseEvent
        {
        }
    }
}