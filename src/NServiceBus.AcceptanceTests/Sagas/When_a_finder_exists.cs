namespace NServiceBus.AcceptanceTests.Sagas
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_finder_exists
    {
        [Test]
        public void Should_use_it_to_find_saga()
        {
            var context = Scenario.Define<Context>()
                   .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                   .Done(c => c.FinderUsed)
                   .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            class CustomFinder : IFindSagas<TestSaga.SagaData>.Using<StartSagaMessage>
            {
                public Context Context { get; set; }
                public TestSaga.SagaData FindBy(StartSagaMessage message)
                {
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

        }

        public class StartSagaMessage : IMessage
        {
        }
    }
}