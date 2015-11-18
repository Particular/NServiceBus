namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using EndpointTemplates;
    using AcceptanceTesting;
    using NServiceBus.Features;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_receiving_that_completes_the_saga : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_hydrate_and_complete_the_existing_instance()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                    .WithEndpoint<ReceiveCompletesSagaEndpoint>(b =>
                        {
                            b.When((bus, context) => bus.SendLocal(new StartSagaMessage { SomeId = context.Id }));
                            b.When(context => context.StartSagaMessageReceived, (bus, context) =>
                            {
                                context.AddTrace("CompleteSagaMessage sent");

                                return bus.SendLocal(new CompleteSagaMessage
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
        public async Task Should_ignore_messages_afterwards()
        {
            await Scenario.Define<Context>(c => { c.Id = Guid.NewGuid(); })
                .WithEndpoint<ReceiveCompletesSagaEndpoint>(b =>
                {
                    b.When((bus, c) => bus.SendLocal(new StartSagaMessage
                    {
                        SomeId = c.Id
                    }));
                    b.When(c => c.StartSagaMessageReceived, (bus, c) =>
                    {
                        c.AddTrace("CompleteSagaMessage sent");
                        return bus.SendLocal(new CompleteSagaMessage
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
                .Should(c =>
                {
                    Assert.True(c.AnotherMessageReceived, "AnotherMessage should have been delivered to the handler outside the saga");
                    Assert.False(c.SagaReceivedAnotherMessage, "AnotherMessage should not be delivered to the saga after completion");
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public Guid Id { get; set; }
            public bool StartSagaMessageReceived { get; set; }
            public bool SagaCompleted { get; set; }
            public bool AnotherMessageReceived { get; set; }
            public bool SagaReceivedAnotherMessage { get; set; }
        }

        public class ReceiveCompletesSagaEndpoint : EndpointConfigurationBuilder
        {
            public ReceiveCompletesSagaEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.EnableFeature<TimeoutManager>();
                    b.ExecuteTheseHandlersFirst(typeof(TestSaga10));
                    b.LimitMessageProcessingConcurrencyTo(1); // This test only works if the endpoints processes messages sequentially
                });
            }

            public class TestSaga10 : Saga<TestSagaData10>,
                IAmStartedByMessages<StartSagaMessage>,
                IHandleMessages<CompleteSagaMessage>,
                IHandleMessages<AnotherMessage>
            {
                public Context Context { get; set; }

                public Task Handle(StartSagaMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("Saga started");

                    Data.SomeId = message.SomeId;

                    Context.StartSagaMessageReceived = true;

                    return Task.FromResult(0);
                }

                public Task Handle(CompleteSagaMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("CompleteSagaMessage received");
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                    return Task.FromResult(0);
                }

                public Task Handle(AnotherMessage message, IMessageHandlerContext context)
                {
                    Context.AddTrace("AnotherMessage received");
                    Context.SagaReceivedAnotherMessage = true;
                    return Task.FromResult(0);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData10> mapper)
                {
                    mapper.ConfigureMapping<StartSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<CompleteSagaMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                    mapper.ConfigureMapping<AnotherMessage>(m => m.SomeId)
                        .ToSaga(s => s.SomeId);
                }
            }

            public class TestSagaData10 : IContainSagaData
            {
                public virtual Guid Id { get; set; }
                public virtual string Originator { get; set; }
                public virtual string OriginalMessageId { get; set; }
                public virtual Guid SomeId { get; set; }
            }
        }

        public class CompletionHandler : IHandleMessages<AnotherMessage>
        {
            public Context Context { get; set; }
            public Task Handle(AnotherMessage message, IMessageHandlerContext context)
            {
                Context.AnotherMessageReceived = true;
                return Task.FromResult(0);
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