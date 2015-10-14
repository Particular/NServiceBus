namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Extensibility;
    using NServiceBus.Features;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NUnit.Framework;

    public class When_clearing_saga_timeouts : NServiceBusAcceptanceTest
    {
        [Test]
        public async Task Should_record_the_request_to_clear_in_outbox()
        {
            var context = await Scenario.Define<Context>()
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.When(bus => bus.SendLocalAsync(new PlaceOrder
                {
                    DataId = Guid.NewGuid()
                })))
                .AllowExceptions()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(2, context.NumberOfOps, "Request to clear and a done signal should be in the outbox");
        }

        public class Context : ScenarioContext
        {
            public int NumberOfOps { get; set; }
            public bool Done { get; set; }
        }

        public class NonDtcReceivingEndpoint : EndpointConfigurationBuilder
        {
            public NonDtcReceivingEndpoint()
            {
                EndpointSetup<DefaultServer>(
                    b =>
                    {
                        b.GetSettings().Set("DisableOutboxTransportCheck", true);
                        b.EnableFeature<TimeoutManager>();
                        b.EnableOutbox();
                        b.EnableFeature<TimeoutManager>();
                        b.UsePersistence<FakeOutboxPersistence>();
                        b.RegisterComponents(c => c.ConfigureComponent<FakeOutbox>(DependencyLifecycle.SingleInstance));
                    });
            }

            class DoneHandler : IHandleMessages<SignalDone>
            {
                public Context Context { get; set; }

                public Task Handle(SignalDone message)
                {
                    Context.Done = true;
                    return Task.FromResult(0);
                }
            }

            class PlaceOrderSaga : Saga<PlaceOrderSaga.PlaceOrderSagaData>, IAmStartedByMessages<PlaceOrder>
            {
                public async Task Handle(PlaceOrder message)
                {
                    Data.DataId = message.DataId;

                    await Bus.SendLocalAsync(new SignalDone());
                    MarkAsComplete();
                }

                protected override void ConfigureHowToFindSaga(SagaPropertyMapper<PlaceOrderSagaData> mapper)
                {
                    mapper.ConfigureMapping<PlaceOrderSagaData>(m => m.DataId).ToSaga(s => s.DataId);
                }

                public class PlaceOrderSagaData : ContainSagaData
                {
                    public virtual Guid DataId { get; set; }
                }
            }
        }

        class FakeOutbox : IOutboxStorage
        {
            public FakeOutbox(Context context)
            {
                testContext = context;
            }

            public Task<OutboxMessage> Get(string messageId, ContextBag context)
            {
                return Task.FromResult(default(OutboxMessage));
            }

            public Task Store(OutboxMessage message, OutboxTransaction transaction, ContextBag context)
            {
                testContext.NumberOfOps += message.TransportOperations.Count();
                return Task.FromResult(0);
            }

            public Task SetAsDispatched(string messageId, ContextBag context)
            {
                return Task.FromResult(0);
            }

            public Task<OutboxTransaction> BeginTransaction(ContextBag context)
            {
                return Task.FromResult<OutboxTransaction>(new FakeOutboxTransaction());
            }

            Context testContext;

            class FakeOutboxTransaction : OutboxTransaction
            {
                public void Dispose()
                {
                }

                public Task Commit()
                {
                    return Task.FromResult(0);
                }
            }
        }


        public class PlaceOrder : ICommand
        {
            public Guid DataId { get; set; }
        }

        public class SignalDone : ICommand
        {
        }
    }

    public class FakeOutboxPersistence : PersistenceDefinition
    {
        public FakeOutboxPersistence()
        {
            Supports<StorageType.Outbox>(s => { });
        }
    }
}