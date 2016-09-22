namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;
    using ScenarioDescriptors;

    public class When_saga_handles_messages_concurrently : NServiceBusAcceptanceTest
    {
        [Test]
        public Task Should_complete_saga_eventually()
        {
            return Scenario.Define<Context>(c => { c.SomeId = Guid.NewGuid().ToString(); })
                .WithEndpoint<ConcurrentHandlerEndpoint>(b =>
                {
                    b.When((session, context) =>
                    {
                        var t1 = session.SendLocal(new StartMessageOne
                        {
                            SomeId = context.SomeId
                        });
                        var t2 = session.SendLocal(new StartMessageTwo
                        {
                            SomeId = context.SomeId
                        });
                        return Task.WhenAll(t1, t2);
                    });
                })
                .Done(c => c.PlacedSagaId != Guid.Empty && c.BilledSagaId != Guid.Empty)
                .Repeat(r => r.For(Transports.Default))
                .Should(c =>
                {
                    Assert.AreNotEqual(Guid.Empty, c.PlacedSagaId);
                    Assert.AreNotEqual(Guid.Empty, c.BilledSagaId);
                    Assert.AreEqual(c.PlacedSagaId, c.BilledSagaId, "Both messages should have been handled by the same saga, but SagaIds don't match.");
                    Assert.True(c.SagaCompleted);
                })
                .Run();
        }

        public class Context : ScenarioContext
        {
            public string SomeId { get; set; }
            public Guid PlacedSagaId { get; set; }
            public Guid BilledSagaId { get; set; }
            public bool SagaCompleted { get; set; }
        }

        public class ConcurrentHandlerEndpoint : EndpointConfigurationBuilder
        {
            public ConcurrentHandlerEndpoint()
            {
                EndpointSetup<DefaultServer>(b =>
                {
                    b.LimitMessageProcessingConcurrencyTo(2);
                    b.Recoverability().Immediate(immediate => immediate.NumberOfRetries(3));
                });
            }

            public class ConcurrentlyStartedSaga : Saga<ConcurrentlyStartedSagaData>,
                IAmStartedByMessages<StartMessageTwo>,
                IAmStartedByMessages<StartMessageOne>
            {
                public Context Context { get; set; }

                public async Task Handle(StartMessageOne message, IMessageHandlerContext context)
                {
                    Data.Placed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageOne)
                    });
                    await CheckForCompletion(context);
                }

                public async Task Handle(StartMessageTwo message, IMessageHandlerContext context)
                {
                    Data.Billed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageTwo)
                    });
                    await CheckForCompletion(context);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ConcurrentlyStartedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartMessageOne>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                    mapper.ConfigureMapping<StartMessageTwo>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                }

                Task CheckForCompletion(IMessageHandlerContext context)
                {
                    if (Data.Billed && Data.Placed)
                    {
                        MarkAsComplete();
                        Context.SagaCompleted = true;
                    }
                    return Task.FromResult(0);
                }
            }

            public class ConcurrentlyStartedSagaData : ContainSagaData
            {
                public virtual string OrderId { get; set; }
                public virtual bool Placed { get; set; }
                public virtual bool Billed { get; set; }
            }

            public class LogSuccessfulHandler : IHandleMessages<SuccessfulProcessing>
            {
                public Context Context { get; set; }

                public Task Handle(SuccessfulProcessing message, IMessageHandlerContext context)
                {
                    if (message.Type == nameof(StartMessageOne))
                    {
                        Context.PlacedSagaId = message.SagaId;
                    }
                    else if (message.Type == nameof(StartMessageTwo))
                    {
                        Context.BilledSagaId = message.SagaId;
                    }
                    else
                    {
                        throw new Exception("Unknown type");
                    }

                    return Task.FromResult(0);
                }
            }
        }

        public class StartMessageOne : ICommand
        {
            public string SomeId { get; set; }
        }

        public class StartMessageTwo : ICommand
        {
            public string SomeId { get; set; }
        }

        public class SuccessfulProcessing : ICommand
        {
            public string Type { get; set; }
            public Guid SagaId { get; set; }
        }
    }
}