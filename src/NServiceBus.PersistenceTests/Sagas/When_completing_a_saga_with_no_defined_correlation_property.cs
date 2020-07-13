namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_completing_a_saga_with_no_defined_correlation_property : SagaPersisterTests<When_completing_a_saga_with_no_defined_correlation_property.SagaWithoutCorrelationProperty, When_completing_a_saga_with_no_defined_correlation_property.SagaWithoutCorrelationPropertyData>
    {
        /// <summary>
        /// There can be a saga that is only started by a message and then is driven by timeouts only.
        /// This kind of saga would not require to be correlated by any property.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task It_should_successfully_remove_the_saga()
        {
            configuration.RequiresFindersSupport(); 

            var propertyData = Guid.NewGuid().ToString();

            var sagaData = new SagaWithoutCorrelationPropertyData {FoundByFinderProperty = propertyData, DateTimeProperty = DateTime.UtcNow};

            var finder = typeof(CustomFinder);

            await SaveSaga(sagaData, finder);

            await GetByIdAndComplete(sagaData.Id, finder);

            var result = await GetById(sagaData.Id, finder);
            Assert.That(result, Is.Null);
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
        
        public class SagaCorrelationPropertyStartingMessage
        {
            public string CorrelatedProperty { get; set; }
        }
    }
}