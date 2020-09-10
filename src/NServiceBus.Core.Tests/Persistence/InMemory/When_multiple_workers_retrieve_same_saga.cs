namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_multiple_workers_retrieve_same_saga
    {
        [Test]
        public async Task Persister_returns_different_instance_of_saga_data()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag(), CancellationToken.None);
            await insertSession.CompleteAsync(CancellationToken.None);

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag(), CancellationToken.None);
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag(), CancellationToken.None);
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update()
        {
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData
            {
                Id = sagaId,
                SomeId = sagaId.ToString()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag(), CancellationToken.None);
            await insertSession.CompleteAsync(CancellationToken.None);

            var winningContext = new ContextBag();
            var losingContext = new ContextBag();
            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), winningContext, CancellationToken.None));
            var returnedSaga2 = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), losingContext, CancellationToken.None);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();
            await persister.Update(returnedSaga1, winningSaveSession, winningContext, CancellationToken.None);
            await persister.Update(returnedSaga2, losingSaveSession, losingContext, CancellationToken.None);

            await winningSaveSession.CompleteAsync(CancellationToken.None);

            Assert.That(async () => await losingSaveSession.CompleteAsync(CancellationToken.None), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData
            {
                Id = sagaId,
                SomeId = sagaId.ToString()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag(), CancellationToken.None);
            await insertSession.CompleteAsync(CancellationToken.None);

            var winningContext = new ContextBag();
            var record = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), winningContext, CancellationToken.None);
            var losingContext = new ContextBag();
            var staleRecord = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), losingContext, CancellationToken.None);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(record, winningSaveSession, winningContext, CancellationToken.None);
            await persister.Update(staleRecord, losingSaveSession, losingContext, CancellationToken.None);

            await winningSaveSession.CompleteAsync(CancellationToken.None);

            Assert.That(async () => await losingSaveSession.CompleteAsync(CancellationToken.None), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_writing_same_data_twice()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag(), CancellationToken.None);
            await insertSession.CompleteAsync(CancellationToken.None);

            var retrievingContext = new ContextBag();
            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), retrievingContext, CancellationToken.None);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga1, winningSaveSession, retrievingContext, CancellationToken.None);
            await persister.Update(returnedSaga1, losingSaveSession, retrievingContext, CancellationToken.None);

            await winningSaveSession.CompleteAsync(CancellationToken.None);

            Assert.That(async () => await losingSaveSession.CompleteAsync(CancellationToken.None), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_process_is_repeatable()
        {
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData
            {
                Id = sagaId,
                SomeId = sagaId.ToString()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag(), CancellationToken.None);
            await insertSession.CompleteAsync(CancellationToken.None);

            var winningSessionContext = new ContextBag();
            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), winningSessionContext, CancellationToken.None));

            var losingSessionContext = new ContextBag();
            var returnedSaga2 = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), losingSessionContext, CancellationToken.None);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga1, winningSaveSession, winningSessionContext, CancellationToken.None);
            await persister.Update(returnedSaga2, losingSaveSession, losingSessionContext, CancellationToken.None);

            await winningSaveSession.CompleteAsync(CancellationToken.None);
            Assert.That(async () => await losingSaveSession.CompleteAsync(CancellationToken.None), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));

            losingSessionContext = new ContextBag();
            var returnedSaga3 = await Task.Run(() => persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), losingSessionContext, CancellationToken.None));

            winningSessionContext = new ContextBag();
            var returnedSaga4 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), winningSessionContext, CancellationToken.None);

            winningSaveSession = new InMemorySynchronizedStorageSession();
            losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga4, winningSaveSession, winningSessionContext, CancellationToken.None);
            await persister.Update(returnedSaga3, losingSaveSession, losingSessionContext, CancellationToken.None);

            await winningSaveSession.CompleteAsync(CancellationToken.None);

            Assert.That(async () => await losingSaveSession.CompleteAsync(CancellationToken.None), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }
    }
}