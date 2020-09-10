namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_has_a_non_empty_constructor : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_hydrate_and_invoke_the_existing_instance()
        {
            return Scenario.Define<Context>()
                .WithEndpoint<NonEmptySagaCtorEndpt>(b => b.When(session => session.SendLocal(new StartSagaMessage
                {
                    SomeId = IdThatSagaIsCorrelatedOn
                })))
                .Done(c => c.SecondMessageReceived)
                .Run();
        }

        static Guid IdThatSagaIsCorrelatedOn = Guid.NewGuid();

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
                public TestSaga11(Context testContext)
                {
                    this.testContext = testContext;
                }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.SomeId = message.SomeId;
                    return context.SendLocal(new OtherMessage
                    {
                        SomeId = message.SomeId
                    });
                }

                public Task Handle(OtherMessage message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    testContext.SecondMessageReceived = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData11> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<OtherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                Context testContext;
            }

            public class TestSagaData11 : IContainSagaData
            {
                public virtual Guid SomeId { get; set; }
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
            }
        }


        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }


        public class OtherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}