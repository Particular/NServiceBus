namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using System.Transactions;
using NUnit.Framework;
using Transport;

public class When_persisting_a_saga_with_an_escalated_DTC_transaction : SagaPersisterTests
{
    [Test]
    public async Task Should_rollback_when_the_dtc_transaction_is_aborted()
    {
        configuration.RequiresDtcSupport();

        var startingSagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString(), LastUpdatedBy = "Unchanged" };
        await SaveSaga(startingSagaData);

        // This enlistment notifier emulates a participating DTC transaction that fails to commit.
        var enlistmentNotifier = new EnlistmentNotifier(abortTransaction: true);

        Assert.That(async () =>
        {
            using var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

            Transaction.Current.EnlistDurable(EnlistmentNotifier.Id, enlistmentNotifier, EnlistmentOptions.None);

            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(Transaction.Current);

            await using var session = configuration.CreateStorageSession();
            var contextBag = configuration.GetContextBagForSagaStorage();

            await session.TryOpen(transportTransaction, contextBag);

            var sagaData = await configuration.SagaStorage.Get<TestSagaData>(startingSagaData.Id, session, contextBag);
            sagaData.LastUpdatedBy = "Changed";
            await configuration.SagaStorage.Update(sagaData, session, contextBag);

            await session.CompleteAsync();

            // When the enlistmentNotifier forces a rollback, the persister should also rollback with the rest of the DTC transaction.
            tx.Complete();
        }, Throws.Exception.TypeOf<TransactionAbortedException>());

        var updatedSagaData = await GetById<TestSagaData>(startingSagaData.Id);

        Assert.That(updatedSagaData, Is.Not.Null);
        Assert.That(updatedSagaData.LastUpdatedBy, Is.EqualTo("Unchanged"));
    }

    [Test]
    public async Task Should_rollback_dtc_transaction_when_storage_session_rolls_back()
    {
        configuration.RequiresDtcSupport();

        var startingSagaData = new TestSagaData { SomeId = Guid.NewGuid().ToString(), LastUpdatedBy = "Unchanged" };
        await SaveSaga(startingSagaData);

        var enlistmentNotifier = new EnlistmentNotifier(abortTransaction: false);

        using (var tx = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
        {
            Transaction.Current.EnlistDurable(EnlistmentNotifier.Id, enlistmentNotifier, EnlistmentOptions.None);

            var transportTransaction = new TransportTransaction();
            transportTransaction.Set(Transaction.Current);

            await using var session = configuration.CreateStorageSession();
            var contextBag = configuration.GetContextBagForSagaStorage();

            await session.TryOpen(transportTransaction, contextBag);

            var sagaData = await configuration.SagaStorage.Get<TestSagaData>(startingSagaData.Id, session, contextBag);
            sagaData.LastUpdatedBy = "Changed";
            await configuration.SagaStorage.Update(sagaData, session, contextBag);

            // There is no call to CompleteAsync() here to emulate what would happen if Update() threw an exception: disposing the TransactionScope without completing the transaction
        }

        await enlistmentNotifier.CompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(30));

        var notUpdatedSagaData = await GetById<TestSagaData>(startingSagaData.Id);

        Assert.That(notUpdatedSagaData, Is.Not.Null);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(notUpdatedSagaData.LastUpdatedBy, Is.EqualTo("Unchanged"));
            Assert.That(enlistmentNotifier.CommitWasCalled, Is.False);
            Assert.That(enlistmentNotifier.RollbackWasCalled, Is.True);
        }
    }

    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context)
            => throw new NotImplementedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            => mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
    }

    public class TestSagaData : ContainSagaData
    {
        public string SomeId { get; set; }

        public string LastUpdatedBy { get; set; }
    }

    public class StartMessage
    {
        public string SomeId { get; set; }
    }

    class EnlistmentNotifier(bool abortTransaction) : IEnlistmentNotification
    {
        public TaskCompletionSource CompletionSource { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool RollbackWasCalled { get; private set; }

        public bool CommitWasCalled { get; private set; }

        public void Prepare(PreparingEnlistment preparingEnlistment)
        {
            if (!abortTransaction)
            {
                preparingEnlistment.Prepared();
            }
            else
            {
                preparingEnlistment.ForceRollback();
            }
        }

        public void Commit(Enlistment enlistment)
        {
            CommitWasCalled = true;
            CompletionSource.SetResult();
            enlistment.Done();
        }

        public void Rollback(Enlistment enlistment)
        {
            RollbackWasCalled = true;
            CompletionSource.SetResult();
            enlistment.Done();
        }

        public void InDoubt(Enlistment enlistment)
        {
            CompletionSource.SetResult();
            enlistment.Done();
        }

        public static readonly Guid Id = Guid.NewGuid();
    }

    public When_persisting_a_saga_with_an_escalated_DTC_transaction(TestVariant param) : base(param)
    {
    }
}