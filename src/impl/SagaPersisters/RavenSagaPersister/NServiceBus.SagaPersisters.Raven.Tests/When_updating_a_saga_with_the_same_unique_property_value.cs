using System;
using NUnit.Framework;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public class When_updating_a_saga_with_the_same_unique_property_value : Raven_saga_persistence_concern
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var saga1 = new SagaWithUniqueProperty
                        {
                            Id = Guid.NewGuid(),
                            UniqueString = "whatever"
                        };

            WithASagaPersistenceUnitOfWork(p => p.Save(saga1));

            WithASagaPersistenceUnitOfWork(p =>
            {
                saga1 = p.Get<SagaWithUniqueProperty>(saga1.Id);
                p.Update(saga1);
            });
        }
    }
}