#pragma warning disable 1591
namespace NServiceBus.PersistenceTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_loaded_by_id : SagaPersisterTests<TestSaga, TestSagaData>
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();

            var saga = new TestSagaData {SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow};

            await SaveSaga(saga);

            var sagaData = await GetByIdAndComplete(saga.Id);

            var completedSaga = await GetById(saga.Id);

            Assert.NotNull(sagaData);
            Assert.Null(completedSaga);
        }
    }
}