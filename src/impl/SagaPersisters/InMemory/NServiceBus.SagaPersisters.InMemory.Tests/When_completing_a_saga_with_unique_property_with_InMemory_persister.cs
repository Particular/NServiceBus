﻿using System;
using NServiceBus.Saga;
using NServiceBus.Serializers.Binary;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    class When_completing_a_saga_with_unique_property_with_InMemory_persister
    {
        [TestFixtureSetUp]
        public void FixtureSetUp()
        {
            InMemorySagaPersister.ConfigureSerializer = () => { return new MessageSerializer(); };
        }

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
