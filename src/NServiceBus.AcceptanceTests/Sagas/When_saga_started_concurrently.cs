namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using AcceptanceTesting;
    using EndpointTemplates;
    using NUnit.Framework;

    public class When_saga_started_concurrently : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_start_single_saga()
        {
            var context = await Scenario.Define<Context>(c => { c.SomeId = Guid.NewGuid().ToString(); })
                .WithEndpoint<ConcurrentHandlerEndpoint>(b =>
                {
                    b.When((session, ctx) =>
                    {
                        var t1 = session.SendLocal(new StartMessageOne
                        {
                            SomeId = ctx.SomeId
                        });
                        var t2 = session.SendLocal(new StartMessageTwo
                        {
                            SomeId = ctx.SomeId
                        });
                        return Task.WhenAll(t1, t2);
                    });
                })
                .Done(c => c.PlacedSagaId != Guid.Empty && c.BilledSagaId != Guid.Empty)
                .Run();

            Assert.AreNotEqual(Guid.Empty, context.PlacedSagaId);
            Assert.AreNotEqual(Guid.Empty, context.BilledSagaId);
            Assert.AreEqual(context.PlacedSagaId, context.BilledSagaId, "Both messages should have been handled by the same saga, but SagaIds don't match.");
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
                public ConcurrentlyStartedSaga(Context context)
                {
                    testContext = context;
                }

                public async Task Handle(StartMessageOne message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.Placed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageOne)
                    });
                    CheckForCompletion(context);
                }

                public async Task Handle(StartMessageTwo message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    Data.Billed = true;
                    await context.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageTwo)
                    });
                    CheckForCompletion(context);
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ConcurrentlyStartedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartMessageOne>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                    mapper.ConfigureMapping<StartMessageTwo>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                }

                void CheckForCompletion(IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (!Data.Billed || !Data.Placed)
                    {
                        return;
                    }
                    MarkAsComplete();
                    testContext.SagaCompleted = true;
                }

                Context testContext;
            }

            public class ConcurrentlyStartedSagaData : ContainSagaData
            {
                public virtual string OrderId { get; set; }
                public virtual bool Placed { get; set; }
                public virtual bool Billed { get; set; }
            }

            // Intercepts the messages sent out by the saga
            class LogSuccessfulHandler : IHandleMessages<SuccessfulProcessing>
            {
                public LogSuccessfulHandler(Context context)
                {
                    testContext = context;
                }

                public Task Handle(SuccessfulProcessing message, IMessageHandlerContext context, System.Threading.CancellationToken cancellationToken)
                {
                    if (message.Type == nameof(StartMessageOne))
                    {
                        testContext.PlacedSagaId = message.SagaId;
                    }
                    else if (message.Type == nameof(StartMessageTwo))
                    {
                        testContext.BilledSagaId = message.SagaId;
                    }
                    else
                    {
                        throw new Exception("Unknown type");
                    }

                    return Task.FromResult(0);
                }

                Context testContext;
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