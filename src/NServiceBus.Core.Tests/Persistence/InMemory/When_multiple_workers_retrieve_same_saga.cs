namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    class When_multiple_workers_retrieve_same_saga
    {
        SagaPersistenceOptions options;

        [SetUp]
        public void SetUp()
        {
            options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(TestSaga)));
        }

        [Test]
        public async Task Persister_returns_different_instance_of_saga_data()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            await persister.Save(saga, options);

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, options);
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, options);
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            await persister.Save(saga, options);

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, options));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, options);

            await persister.Save(returnedSaga1, options);
            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, options));
            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            await persister.Save(saga, options);

            var record = await persister.Get<TestSagaData>(saga.Id, options);
            var staleRecord = await persister.Get<TestSagaData>("Id", saga.Id, options);

            await persister.Save(record, options);
            var exception = Assert.Throws<Exception>(() => persister.Save(staleRecord, options));
            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_fails_when_writing_same_data_twice()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            await persister.Save(saga, options);

            var returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, options);
            await persister.Save(returnedSaga1, options);

            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga1, options));

            Assert.IsTrue(exception.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }

        [Test]
        public async Task Save_process_is_repeatable()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            await persister.Save(saga, options);

            var returnedSaga1 = await Task.Run(() => persister.Get<TestSagaData>(saga.Id, options));
            var returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, options);

            await persister.Save(returnedSaga1, options);
            var exceptionFromSaga2 = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, options));
            Assert.IsTrue(exceptionFromSaga2.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));

            var returnedSaga3 = await Task.Run(() => persister.Get<TestSagaData>("Id", saga.Id, options));
            var returnedSaga4 = await persister.Get<TestSagaData>(saga.Id, options);

            await persister.Save(returnedSaga4, options);

            var exceptionFromSaga3 = Assert.Throws<Exception>(() => persister.Save(returnedSaga3, options));
            Assert.IsTrue(exceptionFromSaga3.Message.StartsWith($"InMemorySagaPersister concurrency violation: saga entity Id[{saga.Id}] already saved."));
        }
    }
}