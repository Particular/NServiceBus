using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_persisting_a_saga_entity_with_a_DateTime_property
    {

        [Test]
        public void Datetime_property_should_be_persisted()
        {
            var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                DateTimeProperty = DateTime.Parse("12/02/2010 12:00:00.01")
            };
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            session.Begin();
            persister.Save(entity);
            session.End();

            session.Begin();
            var savedEntity = persister.Get<SagaData>(entity.Id);
            Assert.AreEqual(entity.DateTimeProperty, savedEntity.DateTimeProperty);

        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public DateTime DateTimeProperty { get; set; }
        }
    }
}