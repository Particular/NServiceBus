namespace NServiceBus.AcceptanceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Config;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_saga_started_concurrently : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_start_single_saga()
        {
            var context = new Context
            {
                SomeId = Guid.NewGuid().ToString()
            };

            Scenario.Define(context)
                .WithEndpoint<ConcurrentHandlerEndpoint>(b =>
                {
                    b.When(bus =>
                    {
                        Parallel.Invoke(() =>
                        {
                            bus.SendLocal(new StartMessageOne
                            {
                                SomeId = context.SomeId
                            });
                        }, () =>
                        {
                            bus.SendLocal(new StartMessageTwo
                                {
                                    SomeId = context.SomeId
                                }
                            );
                        });
                    });
                })
                .AllowExceptions()
                .Done(c => c.PlacedSagaId != Guid.Empty && c.BilledSagaId != Guid.Empty)
                .Run();

            Assert.AreNotEqual(Guid.Empty, context.PlacedSagaId);
            Assert.AreNotEqual(Guid.Empty, context.BilledSagaId);
            Assert.AreEqual(context.PlacedSagaId, context.BilledSagaId, "Both messages should have been handled by the same saga, but SagaIds don't match.");
        }

        class Context : ScenarioContext
        {
            public string SomeId { get; set; }
            public Guid PlacedSagaId { get; set; }
            public Guid BilledSagaId { get; set; }
            public bool SagaCompleted { get; set; }
        }

        class ConcurrentHandlerEndpoint : EndpointConfigurationBuilder
        {
            public ConcurrentHandlerEndpoint()
            {
                EndpointSetup<DefaultServer>(b => { })
                    .WithConfig<TransportConfig>(c =>
                    {
                        c.MaxRetries = 3;
                        c.MaximumConcurrencyLevel = 2;
                    })
                    .WithConfig<SecondLevelRetriesConfig>(c =>
                    {
                        c.Enabled = false;
                    });
            }

            class ConcurrentlyStartedSaga : Saga<ConcurrentlyStartedSagaData>,
                IAmStartedByMessages<StartMessageTwo>,
                IAmStartedByMessages<StartMessageOne>
            {
                public Context Context { get; set; }

                public void Handle(StartMessageOne message)
                {
                    Data.OrderId = message.SomeId;
                    Data.Placed = true;
                    Bus.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageOne)
                    });
                    CheckForCompletion();
                }

                public void Handle(StartMessageTwo message)
                {
                    Data.OrderId = message.SomeId;
                    Data.Billed = true;
                    Bus.SendLocal(new SuccessfulProcessing
                    {
                        SagaId = Data.Id,
                        Type = nameof(StartMessageTwo)
                    });
                    CheckForCompletion();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<ConcurrentlyStartedSagaData> mapper)
                {
                    mapper.ConfigureMapping<StartMessageOne>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                    mapper.ConfigureMapping<StartMessageTwo>(msg => msg.SomeId).ToSaga(saga => saga.OrderId);
                }

                void CheckForCompletion()
                {
                    if (!Data.Billed || !Data.Placed)
                    {
                        return;
                    }
                    MarkAsComplete();
                    Context.SagaCompleted = true;
                }
            }

            class ConcurrentlyStartedSagaData : ContainSagaData
            {
                [Unique]
                public virtual string OrderId { get; set; }
                public virtual bool Placed { get; set; }
                public virtual bool Billed { get; set; }
            }

            // Intercepts the messages sent out by the saga
            class LogSuccessfulHandler : IHandleMessages<SuccessfulProcessing>
            {
                public Context Context { get; set; }

                public void Handle(SuccessfulProcessing message)
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
                }
            }
        }

        [Serializable]
        class StartMessageOne : ICommand
        {
            public string SomeId { get; set; }
        }

        [Serializable]
        class StartMessageTwo : ICommand
        {
            public string SomeId { get; set; }
        }

        [Serializable]
        class SuccessfulProcessing : ICommand
        {
            public string Type { get; set; }
            public Guid SagaId { get; set; }
        }
    }
}