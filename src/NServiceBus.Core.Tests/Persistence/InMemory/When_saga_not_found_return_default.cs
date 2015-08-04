namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
    using NUnit.Framework;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        SagaMetadata metadata;

        [SetUp]
        public void SetUp()
        {
            metadata = SagaMetadata.Create(typeof(SimpleSagaEntitySaga));
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = persister.Get<SimpleSagaEntity>(metadata, "propertyNotFound", null);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = persister.Get<SimpleSagaEntity>(metadata, Guid.Empty);
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
            persister.Save(metadata, simpleSagaEntity);

            var anotherSagaEntity = persister.Get<AnotherSimpleSagaEntity>(metadata, id);
            Assert.IsNull(anotherSagaEntity);
        }
    }
}
