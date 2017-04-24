namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_updating_a_saga_with_no_defined_unique_property : SagaPersisterTests<SagaWithoutCorrelationProperty, SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData,
                DateTimeProperty = DateTime.UtcNow
            };

            var finder = typeof(CustomFinder);

            await SaveSaga(sagaData, finder);
            
            var updateValue = Guid.NewGuid().ToString();
            await GetByIdAndUpdate(sagaData.Id, saga => { saga.FoundByFinderProperty = updateValue; }, finder);

            var result = await GetById(sagaData.Id, finder);
            
            Assert.That(result, Is.Not.Null);
            Assert.That(result.FoundByFinderProperty, Is.EqualTo(updateValue));
        }
    }
}