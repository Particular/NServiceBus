namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    class When_multiple_workers_retrieve_same_saga
    {
        [Test]
        public void Persister_returns_different_instance_of_saga_data()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga);

            var returnedSaga1 = persister.Get<TestSagaData>(saga.Id);
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id);
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public void Save_fails_when_data_changes_between_read_and_update()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga);

            var returnedSaga1 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>(saga.Id)).Result;
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id);

            persister.Save(returnedSaga1);
            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga2));
            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_fails_when_data_changes_between_read_and_update_on_same_thread()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga);

            var record = persister.Get<TestSagaData>(saga.Id);
            var staleRecord = persister.Get<TestSagaData>("Id", saga.Id);

            persister.Save(record);
            var exception = Assert.Throws<Exception>(() => persister.Save(staleRecord));
            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_fails_when_writing_same_data_twice()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga);

            var returnedSaga1 = persister.Get<TestSagaData>(saga.Id);
            persister.Save(returnedSaga1);

            var exception = Assert.Throws<Exception>(() => persister.Save(returnedSaga1));

            Assert.IsTrue(exception.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }

        [Test]
        public void Save_process_is_repeatable()
        {
            var saga = new TestSagaData { Id = Guid.NewGuid() };
            var persister = InMemoryPersisterBuilder.Build<TestSaga>();
            persister.Save(saga);

            var returnedSaga1 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>(saga.Id)).Result;
            var returnedSaga2 = persister.Get<TestSagaData>("Id", saga.Id);

            persister.Save(returnedSaga1);
            var exceptionFromSaga2 = Assert.Throws<Exception>(() => persister.Save(returnedSaga2));
            Assert.IsTrue(exceptionFromSaga2.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));

            var returnedSaga3 = Task<TestSagaData>.Factory.StartNew(() => persister.Get<TestSagaData>("Id", saga.Id)).Result;
            var returnedSaga4 = persister.Get<TestSagaData>(saga.Id);

            persister.Save(returnedSaga4);

            var exceptionFromSaga3 = Assert.Throws<Exception>(() => persister.Save(returnedSaga3));
            Assert.IsTrue(exceptionFromSaga3.Message.StartsWith(string.Format("InMemorySagaPersister concurrency violation: saga entity Id[{0}] already saved.", saga.Id)));
        }
    }
}