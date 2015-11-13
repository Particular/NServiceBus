namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_saga_has_a_non_empty_constructor : NServiceBusAcceptanceTest
    {
        static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();

        [Test]
        public async Task Should_hydrate_and_invoke_the_existing_instance()
        {
            await Scenario.Define<Context>()
                    .WithEndpoint<NonEmptySagaCtorEndpt>(b => b.When(bus => bus.SendLocal(new StartSagaMessage { SomeId = IdThatSagaIsCorrelatedOn })))
                    .Done(c => c.SecondMessageReceived)
                    .Repeat(r => r.For(Persistence.Default))
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

        }

        public class NonEmptySagaCtorEndpt : EndpointConfigurationBuilder
        {
            public NonEmptySagaCtorEndpt()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga11 : Saga<TestSagaData11>,
                IAmStartedByMessages<StartSagaMessage>, 
                IHandleMessages<OtherMessage>
            {
                Context testContext;

                public TestSaga11(Context testContext)
                {
                    this.testContext = testContext;
                }
                
                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Data.SomeId = message.SomeId;
                    return context.SendLocal(new OtherMessage { SomeId = message.SomeId });
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData11> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<OtherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public Task Handle(OtherMessage message, IMessageHandlerContext context)
                {
                    testContext.SecondMessageReceived = true;
                    return Task.FromResult(0);
                }
            }

            public class TestSagaData11 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }

        }
        [Serializable]
        public class OtherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}