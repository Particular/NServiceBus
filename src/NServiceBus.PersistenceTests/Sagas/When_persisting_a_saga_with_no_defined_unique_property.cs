namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_persisting_a_saga_with_no_defined_unique_property : SagaPersisterTests<When_persisting_a_saga_with_no_defined_unique_property.SagaWithoutCorrelationProperty, When_persisting_a_saga_with_no_defined_unique_property.SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            // TODO: why do we require this?
            //configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithoutCorrelationPropertyData {FoundByFinderProperty = propertyData, DateTimeProperty = DateTime.UtcNow};

            var finder = typeof(CustomFinder);

            await SaveSaga(sagaData, finder);

            var result = await GetById(sagaData.Id, finder);

            Assert.AreEqual(sagaData.FoundByFinderProperty, result.FoundByFinderProperty);
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
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
                return Task.FromResult(default(SagaWithoutCorrelationPropertyData));
            }
        }
    }
}