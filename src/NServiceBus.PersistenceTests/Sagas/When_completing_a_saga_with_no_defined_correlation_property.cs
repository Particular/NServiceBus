namespace NServiceBus.PersistenceTests.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_completing_a_saga_with_no_defined_correlation_property : SagaPersisterTests<SagaWithoutCorrelationProperty, SagaWithoutCorrelationPropertyData>
    {
        /// <summary>
        /// There can be a saga that is only started by a message and then is driven by timeouts only.
        /// This kind of saga would not require to be correlated by any property.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task It_should_successfully_remove_the_saga()
        {
            // TODO: why do we require this?
            //configuration.RequiresFindersSupport(); 

            var propertyData = Guid.NewGuid().ToString();

            var sagaData = new SagaWithoutCorrelationPropertyData {FoundByFinderProperty = propertyData, DateTimeProperty = DateTime.UtcNow};

            var finder = typeof(CustomFinder);

            await SaveSaga(sagaData, finder);

            await GetByIdAndComplete(sagaData.Id, finder);

            var result = await GetById(sagaData.Id, finder);
            Assert.That(result, Is.Null);
        }
    }
}