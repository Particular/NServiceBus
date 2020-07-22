namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_persisting_a_saga_with_no_mapping : SagaPersisterTests<When_persisting_a_saga_with_no_mapping.SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_persist_successfully_when_finder_exists()
        {
            configuration.RequiresFindersSupport();

            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = Guid.NewGuid().ToString(), 
                DateTimeProperty = DateTime.UtcNow
            };

            await SaveSaga(sagaData);

            var result = await GetById(sagaData.Id);

            Assert.AreEqual(sagaData.FoundByFinderProperty, result.FoundByFinderProperty);
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
            }

            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class SagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string FoundByFinderProperty { get; set; }

            public DateTime DateTimeProperty { get; set; }
        }

        public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
            public string FoundByFinderProperty { get; set; }
        }

        public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
            {
                throw new NotImplementedException();
            }
        }
    }
}