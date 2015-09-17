namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Pipeline;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_finder_exists_and_context_information_added
    {
        [Test]
        public async Task Should_make_context_information_available()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocalAsync(new StartSagaMessage())))
                .Done(c => c.FinderUsed)
                .Run();

            Assert.True(context.FinderUsed);
            Assert.AreEqual(typeof(SagaEndpoint.TestSaga07), context.Metadata.SagaType);
            Assert.AreEqual("SomeData", context.ContextBag.Get<SagaEndpoint.BehaviorWhichAddsThingsToTheContext.State>().SomeData);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public SagaMetadata Metadata { get; set; }
            public ReadOnlyContextBag ContextBag { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c =>
                {
                    c.EnableFeature<TimeoutManager>();
                    c.Pipeline.Register<BehaviorWhichAddsThingsToTheContext.Registration>();
                });
            }

            class CustomFinder : IFindSagas<TestSaga07.SagaData07>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }
                public Task<TestSaga07.SagaData07> FindBy(StartSagaMessage message, SagaPersistenceOptions options)
                {
                    Context.Metadata = options.Metadata;
                    Context.ContextBag = options.Context;
                    Context.FinderUsed = true;
                    return Task.FromResult(default(TestSaga07.SagaData07));
                }
            }

            public class TestSaga07 : Saga<TestSaga07.SagaData07>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message)
                {
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData07> mapper)
                {
                }

                public class SagaData07 : ContainSagaData
                {
                }
            }

            public class BehaviorWhichAddsThingsToTheContext : PhysicalMessageProcessingStageBehavior
            {
                public override Task Invoke(Context context, Func<Task> next)
                {
                    context.Set(new State
                    {
                        SomeData = "SomeData"
                    });

                    return next();
                }

                public class State
                {
                    public string SomeData { get; set; }
                }

                public class Registration : RegisterStep
                {
                    public Registration() : base("BehaviorWhichAddsThingsToTheContext", typeof(BehaviorWhichAddsThingsToTheContext), "BehaviorWhichAddsThingsToTheContext")
                    {
                    }
                }
            }
        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}