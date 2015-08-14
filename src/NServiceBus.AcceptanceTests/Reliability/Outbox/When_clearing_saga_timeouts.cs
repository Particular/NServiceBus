namespace NServiceBus.AcceptanceTests.Reliability.Outbox
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.AcceptanceTesting;
    using NServiceBus.AcceptanceTests.EndpointTemplates;
    using NServiceBus.Configuration.AdvanceExtensibility;
    using NServiceBus.Outbox;
    using NServiceBus.Persistence;
    using NServiceBus.Saga;
    using NUnit.Framework;

    public class When_clearing_saga_timeouts : NServiceBusAcceptanceTest
    {
        [Test]
        public void Should_record_the_request_to_clear_in_outbox()
        {
            var context = new Context();

            Scenario.Define(context)
                .WithEndpoint<NonDtcReceivingEndpoint>(b => b.Given(bus => bus.SendLocal(new PlaceOrder { DataId = Guid.NewGuid() })))
                .AllowExceptions()
                .Done(c => c.Done)
                .Run();

            Assert.AreEqual(1, context.NumberOfOps, "Request to clear should be in the outbox");
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
                        b.EnableOutbox();
                        b.UsePersistence<FakeOutboxPersistence>();
                        b.RegisterComponents(c => c.ConfigureComponent<FakeOutbox>(DependencyLifecycle.SingleInstance));
                    });
            }

            class PlaceOrderSaga : Saga<PlaceOrderSaga.PlaceOrderSagaData>, IAmStartedByMessages<PlaceOrder>
            {
                public Context Context { get; set; }

                public void Handle(PlaceOrder message)
                {
                    Data.DataId = message.DataId;

                    MarkAsComplete();
                    Context.Done = true;
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
            readonly Context context;

            public FakeOutbox(Context context)
            {
                this.context = context;
            }

            public bool TryGet(string messageId, OutboxStorageOptions options, out OutboxMessage message)
            {
                message = null;
                return false;
            }

            public void Store(string messageId, IEnumerable<TransportOperation> transportOperations, OutboxStorageOptions options)
            {
                context.NumberOfOps = transportOperations.Count();
            }

            public void SetAsDispatched(string messageId, OutboxStorageOptions options)
            {

            }
        }


        public class PlaceOrder : ICommand
        {
            public Guid DataId { get; set; }
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
