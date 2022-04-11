namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    public class When_persisting_a_saga_with_an_escalated_DTC_transaction : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_concurrent_instances()
        {
            configuration.RequiresDtcSupport();

            var persister = configuration.SagaStorage;
            var sagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString() };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            Assert.That(async () =>
            {
                using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, new EnlistmentWhichEnforcesDtcEscalation(), EnlistmentOptions.None);

                    var transportTransaction = new TransportTransaction();
                    transportTransaction.Set(Transaction.Current);

                    var enlistedContextBag = configuration.GetContextBagForSagaStorage();
                    using (var enlistedSession = configuration.CreateStorageSession())
                    {
                        var unenlistedContextBag = configuration.GetContextBagForSagaStorage();
                        using (var unenlistedSession = configuration.CreateStorageSession())
                        {
                            await unenlistedSession.Open(unenlistedContextBag);

                            await enlistedSession.TryOpen(transportTransaction, enlistedContextBag);

                            var unenlistedRecord = await persister.Get<TestSagaData>(generatedSagaId, unenlistedSession,
                                unenlistedContextBag);

                            var enlistedRecord = await persister.Get<TestSagaData>(generatedSagaId, enlistedSession,
                                enlistedContextBag);

                            await persister.Update(unenlistedRecord, unenlistedSession, unenlistedContextBag);
                            await persister.Update(enlistedRecord, enlistedSession, enlistedContextBag);

                            await unenlistedSession.CompleteAsync();
                        }

                        tx.Complete();
                    }
                }
            }, Throws.Exception.TypeOf<TransactionAbortedException>());
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        class EnlistmentWhichEnforcesDtcEscalation : IEnlistmentNotification
        {
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                enlistment.Done();
            }

            public static readonly Guid Id = Guid.NewGuid();
        }

        public When_persisting_a_saga_with_an_escalated_DTC_transaction(TestVariant param) : base(param)
        {
        }
    }
}