using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_persisting_a_saga_entity_with_an_Enum_property 
    {
        [Test]
        public void Enums_should_be_persisted()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                Status = StatusEnum.AnotherStatus
            };


            session.Begin();
            persister.Save(entity);
            session.End();
            
            session.Begin();
            var savedEntity = persister.Get<SagaData>(entity.Id);
            Assert.AreEqual(entity.Status, savedEntity.Status);
        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public StatusEnum Status { get; set; }
        }

        public enum StatusEnum
        {
            SomeStatus,
            AnotherStatus
        }

    }
}