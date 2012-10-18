using System;
using NServiceBus.Saga;
using NServiceBus.Serializers.Binary;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    public class When_multiple_workers_retrieve_same_saga
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            InMemorySagaPersister.ConfigureSerializer = () => { return new MessageSerializer(); };
        }

        [Test]
        public void persister_returns_different_instance_of_saga_data()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new TestSaga { Id = Guid.NewGuid() };
            inMemorySagaPersister.Save(saga);

            var returnedSaga1 = inMemorySagaPersister.Get<TestSaga>(saga.Id);
            var returnedSaga2 = inMemorySagaPersister.Get<TestSaga>(saga.Id);
            Assert.True(returnedSaga1 != returnedSaga2);
            Assert.True(returnedSaga1 != saga);
            Assert.True(returnedSaga2 != saga);
        }

        [Test]
        public void save_fails_when_data_changes_between_read_and_update()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new TestSaga { Id = Guid.NewGuid() };
            inMemorySagaPersister.Save(saga);

            var returnedSaga1 = inMemorySagaPersister.Get<TestSaga>(saga.Id);
            var returnedSaga2 = inMemorySagaPersister.Get<TestSaga>(saga.Id);

            inMemorySagaPersister.Save(returnedSaga1);
            Assert.Throws<InvalidOperationException>(() => inMemorySagaPersister.Save(returnedSaga2));
        }
    }
}
