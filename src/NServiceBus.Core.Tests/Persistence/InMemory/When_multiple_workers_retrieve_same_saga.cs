namespace NServiceBus.SagaPersisters.InMemory.Tests
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new ContextBagImpl());
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
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new ContextBagImpl()));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new ContextBagImpl());

            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());
            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl()));
            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var record = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());
            var staleRecord = await persister.Get<TestSagaData>("Id", saga.Id, new ContextBagImpl());

            await persister.Save(record, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());
            var exception = Assert.Throws<Exception>(() => persister.Save(staleRecord, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl()));
            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_writing_same_data_twice()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());
            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl()));

            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_process_is_repeatable()
        {
            var saga = new TestSagaData
            {
                Id = Guid.NewGuid()
            };
            var persister = new InMemorySagaPersister();
            await persister.Save(saga, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, new ContextBagImpl()));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, new ContextBagImpl());

            await persister.Save(returnedSaga1, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());
            var exceptionFromSaga2 = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl()));
            Assert.IsTrue(exceptionFromSaga2.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));

            var returnedSaga3 = await Task.Run(() => persister.Get<TestSagaData>("Id", saga.Id, new ContextBagImpl()));
            var returnedSaga4 = await persister.Get<TestSagaData>(saga.Id, new ContextBagImpl());

            await persister.Save(returnedSaga4, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl());

            var exceptionFromSaga3 = Assert.Throws<Exception>(() => persister.Save(returnedSaga3, SagaMetadataHelper.GetMetadata<TestSaga>(saga), new ContextBagImpl()));
            Assert.IsTrue(exceptionFromSaga3.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }
    }
}