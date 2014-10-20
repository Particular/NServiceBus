using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_persisting_a_saga_entity_with_inherited_property
    {

        [Test]
        public void Inherited_property_classes_should_be_persisted()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            session.Begin();
            var entity = new SagaData
            {
                Id = Guid.NewGuid(),
                PolymorphicRelatedProperty = new PolymorphicProperty
                {
                    SomeInt = 9
                }
            };
            persister.Save(entity);
            session.End();

            session.Begin();
            var savedEntity = persister.Get<SagaData>(entity.Id);
            var expected = (PolymorphicProperty)entity.PolymorphicRelatedProperty;
            var actual = (PolymorphicProperty)savedEntity.PolymorphicRelatedProperty;
            Assert.AreEqual(expected.SomeInt, actual.SomeInt);

        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public PolymorphicPropertyBase PolymorphicRelatedProperty { get; set; }
        }

        public class PolymorphicProperty : PolymorphicPropertyBase
        {
            public int SomeInt { get; set; }
        }

        public class PolymorphicPropertyBase
        {
            public virtual Guid Id { get; set; }
        }

    }
}