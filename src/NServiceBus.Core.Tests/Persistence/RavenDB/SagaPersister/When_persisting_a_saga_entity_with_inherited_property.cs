namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using NUnit.Framework;

    public class When_persisting_a_saga_entity_with_inherited_property : Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        public override void SetupEntity(TestSaga saga)
        {
            entity.PolymorphicRelatedProperty = new PolymorphicProperty {SomeInt = 9};
        }

        [Test]
        public void Inherited_property_classes_should_be_persisted()
        {
            var polymorphicRelatedProperty = (PolymorphicProperty)savedEntity.PolymorphicRelatedProperty;
            var polymorphicProperty = (PolymorphicProperty)entity.PolymorphicRelatedProperty;
            Assert.AreEqual(polymorphicProperty.SomeInt, polymorphicRelatedProperty.SomeInt);
        }
    }
}