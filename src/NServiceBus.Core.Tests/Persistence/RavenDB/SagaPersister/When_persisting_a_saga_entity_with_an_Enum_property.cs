namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using NUnit.Framework;

    public class When_persisting_a_saga_entity_with_an_Enum_property : Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        public override void SetupEntity(TestSaga saga)
        {
            entity.Status = StatusEnum.AnotherStatus;
        }

        [Test]
        public void Enums_should_be_persisted()
        {
            Assert.AreEqual(entity.Status, savedEntity.Status);
        }
    }
}