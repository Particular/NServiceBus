using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using Persistence.InMemory.SagaPersister;
    using Saga;

    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        [Test]
        public void Should_delete_the_saga()
        {
            var inMemorySagaPersister = new InMemorySagaPersister() as ISagaPersister;
            var saga = new SagaWithUniqueProperty { Id = Guid.NewGuid(), UniqueString = "whatever" };

            inMemorySagaPersister.Save(saga);
            Assert.NotNull(inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga.Id));
            inMemorySagaPersister.Complete(saga);
            Assert.Null(inMemorySagaPersister.Get<SagaWithUniqueProperty>(saga.Id));
        }
    }
}
