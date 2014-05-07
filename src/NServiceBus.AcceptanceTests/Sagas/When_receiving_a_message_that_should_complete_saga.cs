namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_a_message_that_completes_the_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_hydrate_and_complete_the_existing_instance()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<SagaEndpoint>(b =>
                        {
                            b.Given((bus, context) => bus.SendLocal(new StartSagaMessage {SomeId = context.Id}));
                            b.When(context => context.StartSagaMessageReceived, (bus, context) => bus.SendLocal(new CompleteSagaMessage { SomeId = context.Id }));
                        })
                    .Done(c => c.SagaCompleted)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.IsNull(c.UnhandledException))

                    .Run();
        }

        [Test]
        public void Should_ignore_messages_afterwards()
        {
            Scenario.Define(() => new Context {Id = Guid.NewGuid()})
                      .WithEndpoint<SagaEndpoint>(b =>
                      {
                          b.Given((bus, context) => bus.SendLocal(new StartSagaMessage { SomeId = context.Id }));
                          b.When(context => context.StartSagaMessageReceived, (bus, context) => bus.SendLocal(new CompleteSagaMessage { SomeId = context.Id }));
                          b.When(context => context.SagaCompleted, (bus, context) => bus.SendLocal(new AnotherMessage { SomeId = context.Id }));
                      })
                    .Done(c => c.AnotherMessageReceived)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c =>
                        {
                            Assert.IsNull(c.UnhandledException);
                            Assert.False(c.SagaReceivedAnotherMessage,"AnotherMessage should not be delivered to the saga after completion");
                        })
                    .Run();
        }

        public class Context : ScenarioContext
        {
            public Exception UnhandledException { get; set; }
            public Guid Id { get; set; }

            public bool StartSagaMessageReceived { get; set; }

            public bool SagaCompleted { get; set; }

            public bool AnotherMessageReceived { get; set; }
            public bool SagaReceivedAnotherMessage { get; set; }
        }

        public class SagaEndpoint : EndpointConfigurationBuilder
        {
            public SagaEndpoint()
            {
                EndpointSetup<DefaultServer>(c => c.UnicastBus().LoadMessageHandlers<First<TestSaga>>());
            }

            public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartSagaMessage>, IHandleMessages<CompleteSagaMessage>, IHandleMessages<AnotherMessage>
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Data.SomeId = message.SomeId;

                    Context.StartSagaMessageReceived = true;
                }

                public override void ConfigureHowToFindSaga()
                {
                    ConfigureMapping<StartSagaMessage>(m=>m.SomeId)
                        .ToSaga(s=>s.SomeId);
                    ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    ConfigureMapping<AnotherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(CompleteSagaMessage message)
                {
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                public void Handle(AnotherMessage message)
                {
                    Context.SagaReceivedAnotherMessage = true;
                }
            }

            public class TestSagaData : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                [Unique]
                public virtual Guid SomeId { get; set; }
            }
        }

        public class CompletionHandler : IHandleMessages<AnotherMessage>
        {
            public Context Context { get; set; }
            public void Handle(AnotherMessage message)
            {
                Context.AnotherMessageReceived = true;
            }
        }

        [Serializable]
        public class StartSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class CompleteSagaMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }

        [Serializable]
        public class AnotherMessage : ICommand
        {
            public Guid SomeId { get; set; }
        }
    }
}