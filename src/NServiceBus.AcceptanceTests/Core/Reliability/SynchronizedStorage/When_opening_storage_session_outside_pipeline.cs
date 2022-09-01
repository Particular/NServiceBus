namespace NServiceBus.AcceptanceTests.Reliability.SynchronizedStorage
{
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using Extensibility;
    using Features;
    using NUnit.Framework;
    using ObjectBuilder;
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

            Assert.True(context.AdaptedSessionIsNullBeforeOpening, "The adapted session was not null before opening the session.");
            Assert.True(context.AdaptedSessionNotNullAfterOpening, "The adapted session was null after opening the session.");
            Assert.True(context.StorageSessionEqual, "The scoped storage session should be equal.");
        }

        public class Context : ScenarioContext
        {
            public bool StorageSessionEqual { get; set; }
            public bool AdaptedSessionIsNullBeforeOpening { get; set; }
            public bool AdaptedSessionNotNullAfterOpening { get; set; }
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
                    context.RegisterStartupTask(b => new MyTask(b.Build<Context>(), b));
                }

                public class MyTask : FeatureStartupTask
                {
                    public MyTask(Context scenarioContext, IBuilder provider)
                    {
                        this.provider = provider;
                        this.scenarioContext = scenarioContext;
                    }

                    protected override async Task OnStart(IMessageSession session)
                    {
                        using (var childBuilder = provider.CreateChildBuilder())
                        using (var completableSynchronizedStorageSession =
                               childBuilder.Build<CompletableSynchronizedStorageSession>())
                        {
                            scenarioContext.AdaptedSessionIsNullBeforeOpening =
                                completableSynchronizedStorageSession.GetAdaptedSession() == null;

                            await completableSynchronizedStorageSession.Open(new ContextBag());

                            scenarioContext.AdaptedSessionNotNullAfterOpening =
                                completableSynchronizedStorageSession.GetAdaptedSession() != null;

                            var synchronizedStorage = childBuilder.Build<SynchronizedStorageSession>();

                            scenarioContext.StorageSessionEqual =
                                completableSynchronizedStorageSession.GetAdaptedSession()
                                    .Equals(synchronizedStorage);

                            await completableSynchronizedStorageSession.CompleteAsync();
                        }

                        scenarioContext.Done = true;
                    }

                    protected override Task OnStop(IMessageSession session) => Task.FromResult(0);

                    readonly Context scenarioContext;
                    readonly IBuilder provider;
                }
            }
        }
    }
}