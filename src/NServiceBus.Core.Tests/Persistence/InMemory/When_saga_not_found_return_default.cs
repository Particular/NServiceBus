namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        [Test]
        public void Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = persister.Get<SimpleSagaEntity>("propertyNotFound", null);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = persister.Get<SimpleSagaEntity>(Guid.Empty);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_id_of_another_type()
        {
            var id = Guid.NewGuid();
            var simpleSagaEntity = new SimpleSagaEntity
            {
                Id = id,
                OrderSource = "CA"
            };
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            persister.Save(simpleSagaEntity);

            var anotherSagaEntity = persister.Get<AnotherSimpleSagaEntity>(id);
            Assert.IsNull(anotherSagaEntity);
        }
    }
}
