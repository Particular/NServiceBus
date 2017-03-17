namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_complex_types : SagaPersisterTests
    {
        [Test]
        public async Task It_should_get_deep_copy()
        {
            var sagaId = Guid.NewGuid();
            var sagaData = new SagaWithComplexTypeEntity
            {
                Id = sagaId,
                Ints = new List<int> { 1, 2 },
                CorrelationProperty = sagaId.ToString()
            };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var correlationProperty = SetActiveSagaInstance(savingContextBag, new SagaWithComplexType(), sagaData);

                await persister.Save(sagaData, correlationProperty, session, savingContextBag);
                await session.CompleteAsync();
            }

            var readingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                var retrieved = await persister.Get<SagaWithComplexTypeEntity>(sagaData.Id, session, readingContextBag);
                SetActiveSagaInstance(readingContextBag, new SagaWithComplexType(), retrieved);

                CollectionAssert.AreEqual(sagaData.Ints, retrieved.Ints);
                Assert.False(ReferenceEquals(sagaData.Ints, retrieved.Ints));
            }
        }
    }
}