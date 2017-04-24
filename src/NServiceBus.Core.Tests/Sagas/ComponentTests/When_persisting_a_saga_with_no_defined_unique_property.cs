namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_no_defined_unique_property : SagaPersisterTests<SagaWithoutCorrelationProperty, SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithoutCorrelationPropertyData { FoundByFinderProperty = propertyData, DateTimeProperty = DateTime.UtcNow };

            var finder = typeof(CustomFinder);

            await SaveSaga(sagaData, finder);

            var result = await GetById(sagaData.Id, finder);

            Assert.AreEqual(sagaData.FoundByFinderProperty, result.FoundByFinderProperty);
        }
    }
}