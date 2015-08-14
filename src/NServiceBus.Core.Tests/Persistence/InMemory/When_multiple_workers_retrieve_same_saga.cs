namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Saga;
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
        public void Persister_returns_different_instance_of_saga_data()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga, options);

            var returnedSaga1 = persister.Get<TestSagaData>(saga.Id.ToString(), options);
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id, options);
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public void Save_fails_when_data_changes_between_read_and_update()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga, options);

            var returnedSaga1 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>(saga.Id.ToString(), options)).Result;
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id, options);

            persister.Save(returnedSaga1, options);
            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, options));
            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga, options);

            var record = persister.Get<TestSagaData>(saga.Id.ToString(), options);
            var staleRecord = persister.Get<TestSagaData>("Id", saga.Id, options);

            persister.Save(record, options);
            var exception = Assert.Throws<Exception>(() => persister.Save(staleRecord, options));
            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_fails_when_writing_same_data_twice()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga, options);

            var returnedSaga1 = persister.Get<TestSagaData>(saga.Id.ToString(), options);
            persister.Save(returnedSaga1, options);

            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga1, options));

            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_process_is_repeatable()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga, options);

            var returnedSaga1 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>(saga.Id.ToString(), options)).Result;
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id, options);

            persister.Save(returnedSaga1, options);
            var exceptionFromSaga2 = Assert.Throws<Exception>(() => persister.Save(returnedSaga2, options));
            Assert.IsTrue(exceptionFromSaga2.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));

            var returnedSaga3 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>("Id", saga.Id, options)).Result;
            var returnedSaga4 = persister.Get<TestSagaData>(saga.Id.ToString(), options);

            persister.Save(returnedSaga4, options);

            var exceptionFromSaga3 = Assert.Throws<Exception>(() => persister.Save(returnedSaga3, options));
            Assert.IsTrue(exceptionFromSaga3.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }
    }
}