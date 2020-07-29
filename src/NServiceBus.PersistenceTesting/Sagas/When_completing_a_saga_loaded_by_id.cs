namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_completing_a_saga_loaded_by_id : SagaPersisterTests
    {
        [Test]
        public async Task Should_delete_the_saga()
        {
            var saga = new TestSagaData { SomeId = Guid.NewGuid().ToString(), DateTimeProperty = DateTime.UtcNow };
            await SaveSaga(saga);

            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                var sagaData = await configuration.SagaStorage.Get<TestSagaData>(saga.Id, completeSession, context);

                await configuration.SagaStorage.Complete(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var completedSaga = await GetById<TestSagaData>(saga.Id);
            Assert.Null(completedSaga);
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }

            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; }

            public DateTime DateTimeProperty { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_completing_a_saga_loaded_by_id(TestVariant param) : base(param)
        {
        }
    }
}