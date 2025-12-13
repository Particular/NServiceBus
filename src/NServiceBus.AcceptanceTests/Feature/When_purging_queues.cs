namespace NServiceBus.AcceptanceTests.Feature;

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
            .Run();

        Assert.That(context.LocalMessageReceived, Is.True);
    }

    class Context : ScenarioContext
    {
        public bool LocalMessageReceived { get; set; }
    }

    class EndpointWithStartupTask : EndpointConfigurationBuilder
    {
        public EndpointWithStartupTask() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.RegisterStartupTask<StartupTask>();
                c.PurgeOnStartup(true);
            });

        class MessageHandler(Context testContext) : IHandleMessages<LocalMessage>
        {
            public Task Handle(LocalMessage message, IMessageHandlerContext context)
            {
                testContext.LocalMessageReceived = true;
                testContext.MarkAsCompleted();
                return Task.CompletedTask;
            }
        }
        class StartupTask : FeatureStartupTask
        {
            protected override Task OnStart(IMessageSession session, CancellationToken cancellationToken = default) => session.SendLocal(new LocalMessage(), cancellationToken);

            protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
        }
    }

    public class LocalMessage : IMessage;
}