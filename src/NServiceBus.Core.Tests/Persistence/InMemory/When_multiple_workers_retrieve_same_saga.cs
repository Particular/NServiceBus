﻿namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag()));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();
            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), winningSaveSession, new ContextBag());
            await persister.Save(returnedSaga2, SagaMetadataHelper.GetMetadata<TestSaga>(saga), losingSaveSession, new ContextBag());

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var record = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());
            var staleRecord = await persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Save(record, SagaMetadataHelper.GetMetadata<TestSaga>(saga), winningSaveSession, new ContextBag());
            await persister.Save(staleRecord, SagaMetadataHelper.GetMetadata<TestSaga>(saga), losingSaveSession, new ContextBag());

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), winningSaveSession, new ContextBag());
            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), losingSaveSession, new ContextBag());

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_process_is_repeatable()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag()));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), winningSaveSession, new ContextBag());
            await persister.Save(returnedSaga2, SagaMetadataHelper.GetMetadata<TestSaga>(saga), losingSaveSession, new ContextBag());

            await winningSaveSession.CompleteAsync();
            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));

            var returnedSaga3 = await Task.Run(() => persister.Get<TestSagaData>("Id", saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag()));
            var returnedSaga4 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), new ContextBag());

            winningSaveSession = new InMemorySynchronizedStorageSession();
            losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Save(returnedSaga4, SagaMetadataHelper.GetMetadata<TestSaga>(saga), winningSaveSession, new ContextBag());
            await persister.Save(returnedSaga3, SagaMetadataHelper.GetMetadata<TestSaga>(saga), losingSaveSession, new ContextBag());

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }
    }
}