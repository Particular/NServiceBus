#pragma warning disable 1591
namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_loaded_by_id : SagaPersisterTests<When_completing_a_saga_loaded_by_id.TestSaga, When_completing_a_saga_loaded_by_id.TestSagaData>
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

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; } = "Test";

            public DateTime DateTimeProperty { get; set; }
        }
        
        public class StartMessage
        {
            public string SomeId { get; set; }
        }
    }
}