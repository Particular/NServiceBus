namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Extensibility;
    using NServiceBus.Pipeline;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_finder_exists_and_context_information_added
    {
        [Test]
        public void Should_make_context_information_available()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                .Done(c => c.FinderUsed)
                .Run();

            Assert.True(context.FinderUsed);
            Assert.AreEqual(typeof(SagaEndpoint.TestSaga), context.Metadata.SagaType);
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
                EndpointSetup<DefaultServer>(c => c.Pipeline.Register<BehaviorWhichAddsThingsToTheContext.Registration>());
            }

            class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }
                public TestSaga.SagaData FindBy(StartSagaMessage message, SagaPersistenceOptions options)
                {
                    Context.Metadata = options.Metadata;
                    Context.ContextBag = options.Context;
                    Context.FinderUsed = true;
                    return null;
                }
            }

            public class TestSaga : Saga<TestSaga.SagaData>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
                {
                }

                public class SagaData : ContainSagaData
                {
                }
            }

            public class BehaviorWhichAddsThingsToTheContext : PhysicalMessageProcessingStageBehavior
            {
                public override void Invoke(Context context, Action next)
                {
                    context.Set(new State
                    {
                        SomeData = "SomeData"
                    });

                    next();
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