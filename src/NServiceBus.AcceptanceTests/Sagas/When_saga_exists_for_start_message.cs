﻿namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_exists_for_start_message : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_invoke_the_existing_instance()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<ExistingSagaInstanceEndpoint>(b => b
                    .When(session => session.SendLocal(new StartSagaMessage
                    {
                        SomeId = Guid.NewGuid()
                    })))
                .Done(c => c.SecondMessageReceived)
                .Run();

            Assert.AreEqual(context.FirstSagaId, context.SecondSagaId, "The same saga instance should be invoked invoked for both messages");
        }

        public class Context : ScenarioContext
        {
            public bool SecondMessageReceived { get; set; }

            public Guid FirstSagaId { get; set; }
            public Guid SecondSagaId { get; set; }
        }

        public class ExistingSagaInstanceEndpoint : EndpointConfigurationBuilder
        {
            public ExistingSagaInstanceEndpoint()
            {
                EndpointSetup<DefaultServer>();
            }

            public class TestSaga05 : Saga<TestSagaData05>, IAmStartedByMessages<StartSagaMessage>
            {
                public Context TestContext { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    if (message.SecondMessage)
                    {
                        TestContext.SecondSagaId = Data.Id;
                        TestContext.SecondMessageReceived = true;
                    }
                    else
                    {
                        TestContext.FirstSagaId = Data.Id;
                        return context.SendLocal(new StartSagaMessage
                        {
                            SomeId = message.SomeId,
                            SecondMessage = true
                        });
                    }

                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData05> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData05 : IContainSagaData
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

            public bool SecondMessage { get; set; }
        }
    }
}