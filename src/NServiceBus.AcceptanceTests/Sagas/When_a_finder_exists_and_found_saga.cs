namespace NServiceBus.AcceptanceTests.Sagas
{
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    public class When_a_finder_exists_and_found_saga
    {
        [Test]
        public void Should_find_saga_and_not_correlate()
        {
            var context = Scenario.Define<Context>()
                .WithEndpoint<SagaEndpoint>(b => b.Given(bus => bus.SendLocal(new StartSagaMessage())))
                .Done(c => c.Completed)
                .Run();

            Assert.True(context.FinderUsed);
        }

        public class Context : ScenarioContext
        {
            public bool FinderUsed { get; set; }
            public bool Completed { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class CustomFinder : IFindSagas<TestSaga08.SagaData08>.Using<SomeOtherMessage>
            {
                // ReSharper disable once MemberCanBePrivate.Global
                public Context Context { get; set; }

                public TestSaga08.SagaData08 FindBy(SomeOtherMessage message, SagaPersistenceOptions options)
                {
                    Context.FinderUsed = true;
                    return new TestSaga08.SagaData08
                           {
                               Property = "jfbsjdfbsdjh"
                           };
                }
            }

            public class TestSaga08 : Saga<TestSaga08.SagaData08>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<SomeOtherMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Bus.SendLocal(new SomeOtherMessage());
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData08> mapper)
                {
                    // not required because of CustomFinder
                }

                public class SagaData08 : ContainSagaData
                {
                    public virtual string Property { get; set; }
                }

                public void Handle(SomeOtherMessage message)
                {
                    Context.Completed = true;
                }
            }

        }

        public class StartSagaMessage : IMessage
        {
        }

        public class SomeOtherMessage : IMessage
        {
        }
    }
}