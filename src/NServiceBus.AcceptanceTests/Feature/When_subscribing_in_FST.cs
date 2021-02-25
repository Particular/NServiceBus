namespace NServiceBus.AcceptanceTests.Feature
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    /// <summary>
    /// We want to test that subscriptions are possible while the receivers (that own the subscription manager) haven't been started yet.
    /// </summary>
    public class When_subscribing_in_FST : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_receive_subscribed_events_native()
        {
            Requires.NativePubSubSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithStartupTask>(e => e
                    .When(s => s.Publish(new LocalEvent())))
                .Done(c => c.LocalEventReceived)
                .Run();

            Assert.IsTrue(context.LocalEventReceived);
        }

        [Test]
        public async Task Should_receive_subscribed_events_mdps()
        {
            Requires.MessageDrivenPubSub();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithStartupTask>(e => e
                    .When(ctx => ctx.EndpointSubscribed, s => s.Publish(new LocalEvent())))
                .Done(c => c.LocalEventReceived)
                .Run();

            Assert.IsTrue(context.LocalEventReceived);
        }

        class Context : ScenarioContext
        {
            public bool EndpointSubscribed { get; set; }
            public bool LocalEventReceived { get; set; }
        }

        class EndpointWithStartupTask : EndpointConfigurationBuilder
        {
            public EndpointWithStartupTask()
            {
                EndpointSetup<DefaultServer>(c =>
                    {
                        c.DisableFeature<AutoSubscribe>();
                        c.OnEndpointSubscribed<Context>((args, ctx) =>
                        {
                            if (args.MessageType.Contains(typeof(LocalEvent).FullName))
                            {
                                ctx.EndpointSubscribed = true;
                            }
                        });
                    },
                    p => p.RegisterPublisherFor<LocalEvent>(typeof(EndpointWithStartupTask)));
            }

            class MessageHandler : IHandleMessages<LocalEvent>
            {
                readonly Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(LocalEvent message, IMessageHandlerContext context)
                {
                    testContext.LocalEventReceived = true;
                    return Task.CompletedTask;
                }
            }

            class FeatureWithStartupTask : Feature
            {
                public FeatureWithStartupTask() => EnableByDefault();

                protected override void Setup(FeatureConfigurationContext context) =>
                    context.RegisterStartupTask(new StartupTask());

                class StartupTask : FeatureStartupTask
                {
                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        return session.Subscribe<LocalEvent>(cancellationToken);
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                }
            }
        }

        public class LocalEvent : IEvent
        {
        }
    }
}