namespace NServiceBus.AcceptanceTests.Core.AutomaticSubscriptions
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_starting_an_endpoint_with_autosubscribe_disabled : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_not_autosubscribe_any_events_on_native_pubsub()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>(e => e
                    .When(async (session, ctx) =>
                    {
                        await session.Subscribe<ManuallySubscribedEvent>();
                        ctx.SubscribedToEvent = true;
                    }))
                .WithEndpoint<Publisher>(e => e
                    .When(
                        ctx => ctx.SubscribedToEvent,
                        async session =>
                        {
                            await session.Publish(new NonSubscribedEvent());
                            await session.Publish(new ManuallySubscribedEvent());
                        }))
                .Done(ctx => ctx.ManuallySubscribedEventReceived)
                .Run();

            Assert.IsTrue(context.ManuallySubscribedEventReceived);
            Assert.IsFalse(context.NonSubscribedEventReceived);
        }

        [Test]
        public async Task Should_not_autosubscribe_any_events()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<Subscriber>(e => e
                    .When(async (session, ctx) =>
                    {
                        await session.Subscribe<ManuallySubscribedEvent>();
                    }))
                .WithEndpoint<Publisher>(e => e.When(
                    ctx => ctx.SubscribedToEvent,
                    async session =>
                    {
                        await session.Publish(new NonSubscribedEvent());
                        await session.Publish(new ManuallySubscribedEvent());
                    }))
                .Done(ctx => ctx.ManuallySubscribedEventReceived)
                .Run();

            Assert.IsTrue(context.ManuallySubscribedEventReceived);
            Assert.IsFalse(context.NonSubscribedEventReceived);
        }

        class Context : ScenarioContext
        {
            public bool SubscribedToEvent { get; set; }
            public bool NonSubscribedEventReceived { get; set; }
            public bool ManuallySubscribedEventReceived { get; set; }
        }

        class Subscriber : EndpointConfigurationBuilder
        {
            public Subscriber()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.DisableFeature<AutoSubscribe>();
                }, publisherMetadata =>
                {
                    publisherMetadata.RegisterPublisherFor<NonSubscribedEvent>(typeof(Publisher));
                    publisherMetadata.RegisterPublisherFor<ManuallySubscribedEvent>(typeof(Publisher));
                });
            }

            class EventHandler : IHandleMessages<NonSubscribedEvent>, IHandleMessages<ManuallySubscribedEvent>
            {
                Context testContext;

                public EventHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(NonSubscribedEvent message, IMessageHandlerContext context)
                {
                    testContext.NonSubscribedEventReceived = true;
                    return Task.CompletedTask;
                }

                public Task Handle(ManuallySubscribedEvent message, IMessageHandlerContext context)
                {
                    testContext.ManuallySubscribedEventReceived = true;
                    return Task.CompletedTask;
                }
            }
        }

        class Publisher : EndpointConfigurationBuilder
        {
            public Publisher()
            {
                EndpointSetup<DefaultServer>(c => c.OnEndpointSubscribed<Context>((args, ctx) =>
                {
                    if (args.MessageType.Contains(nameof(ManuallySubscribedEvent)))
                    {
                        ctx.SubscribedToEvent = true;
                    }
                }));
            }
        }

        class NonSubscribedEvent : IEvent
        {
        }

        class ManuallySubscribedEvent : IEvent
        {
        }
    }
}