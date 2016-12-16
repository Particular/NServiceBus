namespace NServiceBus.Core.Tests.Persistence.InMemory
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    public class When_persisting_a_saga_with_complex_types
    {
        [Test]
        public async Task It_should_get_deep_copy()
        {
            var sagaData = new SagaWithComplexType
            {
                Id = Guid.NewGuid(),
                Ints = new List<int> { 1, 2 }
            };

            var persister = new InMemorySagaPersister();
            using (var session = new InMemorySynchronizedStorageSession())
            {
                await persister.Save(sagaData, null, session, new ContextBag());
                await session.CompleteAsync();
            }

            using (var session = new InMemorySynchronizedStorageSession())
            {
                var retrieved = await persister.Get<SagaWithComplexType>(sagaData.Id, session, new ContextBag());

                CollectionAssert.AreEqual(sagaData.Ints, retrieved.Ints);
                Assert.False(ReferenceEquals(sagaData.Ints, retrieved.Ints));
            }
        }

        class SagaWithComplexType : ContainSagaData
        {
            public List<int> Ints { get; set; }
        }
    }
}