namespace NServiceBus.AcceptanceTests.Feature
{
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Features;
    using NUnit.Framework;

    public class When_purging_queues : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_purge_before_FeatureStartupTasks()
        {
            Requires.PurgeOnStartupSupport();

            var context = await Scenario.Define<Context>()
                .WithEndpoint<EndpointWithStartupTask>()
                .Done(c => c.LocalMessageReceived)
                .Run();

            Assert.IsTrue(context.LocalMessageReceived);
        }

        class Context : ScenarioContext
        {
            public bool LocalMessageReceived { get; set; }
        }

        class EndpointWithStartupTask : EndpointConfigurationBuilder
        {
            public EndpointWithStartupTask()
            {
                EndpointSetup<DefaultServer>(c => c
                    .PurgeOnStartup(true));
            }

            class MessageHandler : IHandleMessages<LocalMessage>
            {
                readonly Context testContext;

                public MessageHandler(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(LocalMessage message, IMessageHandlerContext context)
                {
                    testContext.LocalMessageReceived = true;
                    return Task.CompletedTask;
                }
            }

            class FeatureWithStartupTask : Feature
            {
                public FeatureWithStartupTask() => EnableByDefault();

                protected override Task Setup(FeatureConfigurationContext context, CancellationToken cancellationToken = default)
                {
                    context.RegisterStartupTask(new StartupTask());
                    return Task.CompletedTask;
                }

                class StartupTask : FeatureStartupTask
                {
                    protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        return session.SendLocal(new LocalMessage(), cancellationToken);
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
                }
            }
        }

        public class LocalMessage : IMessage
        {
        }
    }
}