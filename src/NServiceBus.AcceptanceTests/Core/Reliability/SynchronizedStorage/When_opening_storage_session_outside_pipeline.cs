namespace NServiceBus.AcceptanceTests.Reliability.SynchronizedStorage
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using Microsoft.Extensions.DependencyInjection;
    using NUnit.Framework;
    using Persistence;

    public class When_opening_storage_session_outside_pipeline : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_provide_adapted_session_with_same_scope()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<Endpoint>()
                .Done(c => c.Done)
                .Run();

            Assert.True(context.SessionNotNullAfterOpening, "The adapted session was null after opening the session.");
            Assert.True(context.StorageSessionEqual, "The scoped storage session should be equal.");
        }

        public class Context : ScenarioContext
        {
            public bool StorageSessionEqual { get; set; }
            public bool SessionNotNullAfterOpening { get; set; }
            public bool Done { get; set; }
        }

        public class Endpoint : EndpointConfigurationBuilder
        {
            public Endpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<Bootstrapper>();
                });
            }

            public class Bootstrapper : Feature
            {
                public Bootstrapper() => EnableByDefault();

                protected override void Setup(FeatureConfigurationContext context)
                {
                    context.RegisterStartupTask(b => new MyTask(b.GetRequiredService<Context>(), b));
                }

                public class MyTask : FeatureStartupTask
                {
                    public MyTask(Context scenarioContext, IServiceProvider provider)
                    {
                        this.provider = provider;
                        this.scenarioContext = scenarioContext;
                    }

                    protected override async Task OnStart(IMessageSession session, CancellationToken cancellationToken = default)
                    {
                        using (var scope = provider.CreateScope())
                        using (var completableSynchronizedStorageSession =
                               scope.ServiceProvider.GetRequiredService<ICompletableSynchronizedStorageSession>())
                        {
                            await completableSynchronizedStorageSession.Open(new ContextBag(), cancellationToken);

                            scenarioContext.SessionNotNullAfterOpening =
                                scope.ServiceProvider.GetService<ISynchronizedStorageSession>() != null;

                            var synchronizedStorage = scope.ServiceProvider.GetService<ISynchronizedStorageSession>();

                            scenarioContext.StorageSessionEqual =
                                completableSynchronizedStorageSession == synchronizedStorage;

                            await completableSynchronizedStorageSession.CompleteAsync(cancellationToken);
                        }

                        scenarioContext.Done = true;
                    }

                    protected override Task OnStop(IMessageSession session, CancellationToken cancellationToken = default) => Task.CompletedTask;

                    readonly Context scenarioContext;
                    readonly IServiceProvider provider;
                }
            }
        }
    }
}