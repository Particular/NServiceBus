namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using System.Transactions;
    using NUnit.Framework;
    using Transport;

    [TestFixture]
    public class When_persisting_a_saga_with_an_escalated_DTC_transaction : SagaPersisterTests
    {
        [Test]
        public async Task Save_should_fail_when_data_changes_between_concurrent_instances()
        {
            configuration.RequiresDtcSupport();

            var correlationPropertData = Guid.NewGuid().ToString();

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            Guid generatedSagaId;
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var sagaData = new TestSagaData {SomeId = correlationPropertData};
                SetActiveSagaInstanceForSave(savingContextBag, new TestSaga(), sagaData);
                generatedSagaId = sagaData.Id;

                await persister.Save(sagaData, null, session, savingContextBag);
                await session.CompleteAsync();
            }

            Assert.That(async () =>
            {
                var storageAdapter = configuration.SynchronizedStorageAdapter;
                using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
                {
                    Transaction.Current.EnlistDurable(EnlistmentWhichEnforcesDtcEscalation.Id, new EnlistmentWhichEnforcesDtcEscalation(), EnlistmentOptions.None);

                    var transportTransaction = new TransportTransaction();
                    transportTransaction.Set(Transaction.Current);

                    var unenlistedContextBag = configuration.GetContextBagForSagaStorage();
                    using (var unenlistedSession = await configuration.SynchronizedStorage.OpenSession(unenlistedContextBag))
                    {
                        var enlistedContextBag = configuration.GetContextBagForSagaStorage();
                        var enlistedSession = await storageAdapter.TryAdapt(transportTransaction, enlistedContextBag);

                        SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(unenlistedContextBag, new TestSagaData {Id = generatedSagaId, SomeId = correlationPropertData});
                        var unenlistedRecord = await persister.Get<TestSagaData>(generatedSagaId, unenlistedSession, unenlistedContextBag);
                        SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(unenlistedContextBag, unenlistedRecord);

                        SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(enlistedContextBag, new TestSagaData {Id = generatedSagaId, SomeId = correlationPropertData});
                        var enlistedRecord = await persister.Get<TestSagaData>("Id", generatedSagaId, enlistedSession, enlistedContextBag);
                        SetActiveSagaInstanceForGet<TestSaga, TestSagaData>(enlistedContextBag, enlistedRecord);

                        await persister.Update(unenlistedRecord, unenlistedSession, unenlistedContextBag);
                        await persister.Update(enlistedRecord, enlistedSession, enlistedContextBag);

                        await unenlistedSession.CompleteAsync();
                    }

                    tx.Complete();
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
            public string SomeId { get; set; } = "Test";

            public DateTime DateTimeProperty { get; set; }
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
    }
}