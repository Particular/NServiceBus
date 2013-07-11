namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NUnit.Framework;

    public class When_persisting_a_saga_entity_with_a_DateTime_property : Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        public override void SetupEntity(TestSaga saga)
        {
            saga.DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01");
        }

        [Test]
        public void Datetime_property_should_be_persisted()
        {
            Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);
        }
    }
}