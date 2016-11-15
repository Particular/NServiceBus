﻿namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
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
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData
            {
                Id = sagaId,
                SomeId = sagaId.ToString()
            };
            var persister = new InMemorySagaPersister();
            var insertSession = new InMemorySynchronizedStorageSession();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var ctx1 = new ContextBag();
            var ctx2 = new ContextBag();
            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), ctx1));
            var returnedSaga2 = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), ctx2);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();
            await persister.Update(returnedSaga1, winningSaveSession, ctx1);
            await persister.Update(returnedSaga2, losingSaveSession, ctx2);

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var ctx1 = new ContextBag();
            var record = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), ctx1);
            var ctx2 = new ContextBag();
            var staleRecord = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), ctx2);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(record, winningSaveSession, ctx1);
            await persister.Update(staleRecord, losingSaveSession, ctx2);

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

            var ctx = new ContextBag();
            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), ctx);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga1, winningSaveSession, ctx);
            await persister.Update(returnedSaga1, losingSaveSession, ctx);

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), insertSession, new ContextBag());
            await insertSession.CompleteAsync();

            var ctx1 = new ContextBag();
            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), ctx1));

            var ctx2 = new ContextBag();
            var returnedSaga2 = await persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), ctx2);

            var winningSaveSession = new InMemorySynchronizedStorageSession();
            var losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga1, winningSaveSession, ctx1);
            await persister.Update(returnedSaga2, losingSaveSession, ctx2);

            await winningSaveSession.CompleteAsync();
            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));

            var ctx3 = new ContextBag();
            var returnedSaga3 = await Task.Run(() => persister.Get<TestSagaData>("SomeId", sagaId.ToString(), new InMemorySynchronizedStorageSession(), ctx3));

            var ctx4 = new ContextBag();
            var returnedSaga4 = await persister.Get<TestSagaData>(saga.Id, new InMemorySynchronizedStorageSession(), ctx4);

            winningSaveSession = new InMemorySynchronizedStorageSession();
            losingSaveSession = new InMemorySynchronizedStorageSession();

            await persister.Update(returnedSaga4, winningSaveSession, ctx4);
            await persister.Update(returnedSaga3, losingSaveSession, ctx3);

            await winningSaveSession.CompleteAsync();

            Assert.That(async () => await losingSaveSession.CompleteAsync(), Throws.InstanceOf<Exception>().And.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }
    }
}