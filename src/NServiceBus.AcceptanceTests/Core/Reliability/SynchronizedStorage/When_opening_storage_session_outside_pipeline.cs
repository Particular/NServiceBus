namespace NServiceBus.AcceptanceTests.Reliability.SynchronizedStorage;

using System;
using System.Threading;
using System.Threading.Tasks;
using AcceptanceTesting;
using EndpointTemplates;
using Extensibility;
using Features;
using Microsoft.Extensions.DependencyInjection;
using NServiceBus.Configuration.AdvancedExtensibility;
using NServiceBus.Outbox;
using NUnit.Framework;
using Persistence;

public class When_opening_storage_session_outside_pipeline : NServiceBusAcceptanceTest
{
    [Test, Combinatorial]
    public async Task Should_provide_adapted_session_with_same_scope([Values(true, false)] bool useOutbox, [Values(true, false)] bool sendOnly)
    {
        var context = await Scenario.Define<Context>()
            .WithEndpoint<Endpoint>(e => e.CustomConfig(c =>
            {
                if (sendOnly)
                {
                    c.SendOnly();
                    c.GetSettings().Set("Outbox.AllowUseWithoutReceiving", true);
                }
                if (useOutbox)
                {
                    c.ConfigureTransport().TransportTransactionMode = TransportTransactionMode.ReceiveOnly;
                    c.EnableOutbox();
                }
            }))
            .Run();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.SessionNotNullAfterOpening, Is.True, "The adapted session was null after opening the session.");
            Assert.That(context.StorageSessionEqual, Is.True, "The scoped storage session should be equal.");
        }

        if (useOutbox)
        {
            Assert.That(context.OutboxNotNullAfterOpening, Is.True, "The scoped storage session should be equal.");
        }
    }

    public class Context : ScenarioContext
    {
        public bool StorageSessionEqual { get; set; }
        public bool SessionNotNullAfterOpening { get; set; }
        public bool OutboxNotNullAfterOpening { get; set; }
    }

    public class Endpoint : EndpointConfigurationBuilder
    {
        public Endpoint() =>
            EndpointSetup<DefaultServer>(c =>
            {
                c.EnableFeature<Bootstrapper>();
            });

        public class Bootstrapper : Feature
        {
            protected override void Setup(FeatureConfigurationContext context) => context.RegisterStartupTask<MyTask>();

            class MyTask(Context scenarioContext, IServiceProvider provider) : FeatureStartupTask
            {
                protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                {
                    await using (var scope = provider.CreateAsyncScope())
                    await using (var completableSynchronizedStorageSession =
                           scope.ServiceProvider.GetRequiredService<ICompletableSynchronizedStorageSession>())
                    {
                        await completableSynchronizedStorageSession.Open(new ContextBag(), cancellationToken);

                        scenarioContext.SessionNotNullAfterOpening =
                            scope.ServiceProvider.GetService<ISynchronizedStorageSession>() != null;

                        var synchronizedStorage = scope.ServiceProvider.GetService<ISynchronizedStorageSession>();

                        scenarioContext.StorageSessionEqual =
                            completableSynchronizedStorageSession == synchronizedStorage;

                        scenarioContext.OutboxNotNullAfterOpening = scope.ServiceProvider.GetService<IOutboxStorage>() != null;

                        await completableSynchronizedStorageSession.CompleteAsync(cancellationToken);
                    }

                    scenarioContext.MarkAsCompleted();
                }

                protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;
            }
        }
    }
}