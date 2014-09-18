namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NUnit.Framework;
    using Saga;
    using ScenarioDescriptors;

    public class When_receiving_that_completes_the_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_hydrate_and_complete_the_existing_instance()
        {
            Scenario.Define(() => new Context { Id = Guid.NewGuid() })
                    .WithEndpoint<SagaEndpoint>(b =>
                        {
                            b.Given((bus, context) => bus.SendLocal(new StartSagaMessage { SomeId = context.Id }));
                            b.When(context => context.StartSagaMessageReceived, (bus, context) =>
                            {
                                context.AddTrace("CompleteSagaMessage sent");

                                bus.SendLocal(new CompleteSagaMessage
                                {
                                    SomeId = context.Id
                                });
                            });
                        })
                    .Done(c => c.SagaCompleted)
                    .Repeat(r => r.For(Transports.Default))
                    .Should(c => Assert.True(c.SagaCompleted))

                    .Run();
        }

        [Test]
        public void Should_ignore_messages_afterwards()
        {
            var context = new Context
            {
                Id = Guid.NewGuid()
            };

            Scenario.Define(context)
                .WithEndpoint<SagaEndpoint>(b =>
                {
                    b.Given((bus, c) => bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = c.Id
                    }));
                    b.When(c => c.StartSagaMessageReceived, (bus, c) =>
                    {
                        c.AddTrace("CompleteSagaMessage sent");
                        bus.SendLocal(new CompleteSagaMessage
                        {
                            SomeId = c.Id
                        });
                    });
                    b.When(c => c.SagaCompleted, (bus, c) => bus.SendLocal(new AnotherMessage
                    {
                        SomeId = c.Id
                    }));
                })
                .Done(c => c.AnotherMessageReceived)
                .Repeat(r => r.For(Transports.Default))
                .Run();

            Assert.True(context.AnotherMessageReceived, "AnotherMessage should have been delivered to the handler outside the saga");
            Assert.False(context.SagaReceivedAnotherMessage, "AnotherMessage should not be delivered to the saga after completion");
        }

        public class Context : ScenarioContext
        {
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
                EndpointSetup<DefaultServer>(b => b.LoadMessageHandlers<First<TestSaga>>());
            }

            public class TestSaga : Saga<TestSagaData>, 
                IAmStartedByMessages<StartSagaMessage>, 
                IHandleMessages<CompleteSagaMessage>, 
                IHandleMessages<AnotherMessage>,
                IHandleSagaNotFound
            {
                public Context Context { get; set; }

                public void Handle(StartSagaMessage message)
                {
                    Context.AddTrace("Saga started");

                    Data.SomeId = message.SomeId;

                    Context.StartSagaMessageReceived = true;
                }

                public void Handle(CompleteSagaMessage message)
                {
                    Context.AddTrace("CompleteSagaMessage received");
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }

                public void Handle(AnotherMessage message)
                {
                    Context.AddTrace("AnotherMessage received");
                    Context.SagaReceivedAnotherMessage = true;
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<AnotherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }

                public void Handle(object message)
                {
                    if (message is AnotherMessage)
                    {
                        return;
                    }

                    throw new Exception("Unexpected 'saga not found' for message: " + message.GetType().Name);
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