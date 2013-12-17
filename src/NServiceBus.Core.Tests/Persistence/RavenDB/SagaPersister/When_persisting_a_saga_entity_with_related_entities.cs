namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using NUnit.Framework;

    public class When_persisting_a_saga_entity_with_related_entities : Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        public override void SetupEntity(TestSaga saga)
        {
            entity.RelatedClass = new RelatedClass { ParentSaga = entity };
        }

        [Test]
        public void Related_entities_should_also_be_persisted()
        {
            Assert.AreEqual(entity.Id, savedEntity.RelatedClass.ParentSaga.Id);
        }

        [Test]
        public void Self_referenced_properties_should_be_persisted_as_references()
        {
            Assert.AreSame(savedEntity.RelatedClass.ParentSaga, savedEntity);
        }
    }
}