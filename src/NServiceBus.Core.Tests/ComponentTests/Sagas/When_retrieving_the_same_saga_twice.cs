namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_retrieving_the_same_saga_twice : SagaPersisterTests
    {
        [Test]
        public async Task Get_returns_different_instance_of_saga_data()
        {
            var sagaId = Guid.NewGuid();
            var saga = new TestSagaData { Id = sagaId, SomeId = sagaId.ToString() };

            var persister = configuration.SagaStorage;
            var insertContextBag = configuration.GetContextBagForSagaStorage();
            using (var insertSession = await configuration.SynchronizedStorage.OpenSession(insertContextBag))
            {
                var correlationProperty = SetActiveSagaInstance(insertContextBag, new TestSaga(), saga);

                await persister.Save(saga, correlationProperty, insertSession, insertContextBag);
                await insertSession.CompleteAsync();
            }

            TestSagaData returnedSaga1;
            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstance(readContextBag, new TestSaga(), new TestSagaData());

                returnedSaga1 = await persister.Get<TestSagaData>(saga.Id, readSession, readContextBag);

                await readSession.CompleteAsync();
            }

            TestSagaData returnedSaga2;
            readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                SetActiveSagaInstance(readContextBag, new TestSaga(), new TestSagaData());

                returnedSaga2 = await persister.Get<TestSagaData>("Id", saga.Id, readSession, readContextBag);

                await readSession.CompleteAsync();
            }

            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }
    }
}