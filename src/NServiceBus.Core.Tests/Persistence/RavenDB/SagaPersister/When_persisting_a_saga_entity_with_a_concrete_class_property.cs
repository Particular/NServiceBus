namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using NUnit.Framework;

    public class When_persisting_a_saga_entity_with_a_concrete_class_property : Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        public override void SetupEntity(TestSaga saga)
        {
            entity.TestComponent = new TestComponent { Property = "Prop" };
        }

        [Test]
        public void Public_setters_and_getters_of_concrete_classes_should_be_persisted()
        {
            Assert.AreEqual(entity.TestComponent.Property, savedEntity.TestComponent.Property);
        }
    }
}