namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_retrieving_the_same_saga_twice : SagaPersisterTests<TestSaga, TestSagaData>
    {
        [Test]
        public async Task Get_returns_different_instance_of_saga_data()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };

            await SaveSaga(saga);

            var returnedSaga1 = await GetById(saga.Id);
            var returnedSaga2 = await GetById(saga.Id);

            Assert.AreNotSame(returnedSaga2, returnedSaga1);
            Assert.AreNotSame(returnedSaga1, saga);
            Assert.AreNotSame(returnedSaga2, saga);
        }
    }
}