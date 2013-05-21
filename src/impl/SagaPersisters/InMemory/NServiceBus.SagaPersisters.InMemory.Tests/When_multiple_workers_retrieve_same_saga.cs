namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;
    using Persistence;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    class When_multiple_workers_retrieve_same_saga
    {
        [Test]
        public void Persister_returns_different_instance_of_saga_data()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new TestSaga { Id = Guid.NewGuid() };
            inMemorySagaPersister.Save(saga);

            var returnedSaga1 = inMemorySagaPersister.Get<TestSaga>(saga.Id);
            var returnedSaga2 = inMemorySagaPersister.Get<TestSaga>("Id", saga.Id);
            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }

        [Test]
        public void Save_fails_when_data_changes_between_read_and_update()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new TestSaga { Id = Guid.NewGuid() };
            inMemorySagaPersister.Save(saga);

            var returnedSaga1 = Task<TestSaga>.Factory.StartNew(() => inMemorySagaPersister.Get<TestSaga>(saga.Id)).Result;
            var returnedSaga2 = inMemorySagaPersister.Get<TestSaga>("Id", saga.Id);

            inMemorySagaPersister.Save(returnedSaga1);
            Assert.Throws<ConcurrencyException>(() => inMemorySagaPersister.Save(returnedSaga2));
        }

        [Test]
        public void Save_process_is_repeatable()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new TestSaga { Id = Guid.NewGuid() };
            inMemorySagaPersister.Save(saga);

            var returnedSaga1 = Task<TestSaga>.Factory.StartNew(() => inMemorySagaPersister.Get<TestSaga>(saga.Id)).Result;
            var returnedSaga2 = inMemorySagaPersister.Get<TestSaga>("Id", saga.Id);

            inMemorySagaPersister.Save(returnedSaga1);
            Assert.Throws<ConcurrencyException>(() => inMemorySagaPersister.Save(returnedSaga2));

            var returnedSaga3 = Task<TestSaga>.Factory.StartNew(() => inMemorySagaPersister.Get<TestSaga>("Id", saga.Id)).Result;
            var returnedSaga4 = inMemorySagaPersister.Get<TestSaga>(saga.Id);

            inMemorySagaPersister.Save(returnedSaga4);
            Assert.Throws<ConcurrencyException>(() => inMemorySagaPersister.Save(returnedSaga3));
        }
    }
}
