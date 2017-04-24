namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_complex_types : SagaPersisterTests<SagaWithComplexType,SagaWithComplexTypeEntity>
    {
        [Test]
        public async Task It_should_get_deep_copy()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithComplexTypeEntity { Ints = new List<int> { 1, 2 }, CorrelationProperty = correlationPropertyData };

            await SaveSaga(sagaData);

            var retrieved = await GetById(sagaData.Id);

            CollectionAssert.AreEqual(sagaData.Ints, retrieved.Ints);
            Assert.False(ReferenceEquals(sagaData.Ints, retrieved.Ints));
        }
    }
}