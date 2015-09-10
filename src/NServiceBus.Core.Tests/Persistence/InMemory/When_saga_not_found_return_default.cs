namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        SagaPersistenceOptions options;

        [SetUp]
        public void SetUp()
        {
            options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(SimpleSagaEntitySaga)));
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>("propertyNotFound", null, options);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>(Guid.Empty, options);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id_of_another_type()
        {
            var id = Guid.NewGuid();
            var simpleSagaEntity = new SimpleSagaEntity
            {
                Id = id,
                OrderSource = "CA"
            };
            var persister = InMemoryPersisterBuilder.Build<SimpleSagaEntitySaga>();
            await persister.Save(simpleSagaEntity, options);

            var anotherSagaEntity = await persister.Get<AnotherSimpleSagaEntity>(id, options);
            Assert.IsNull(anotherSagaEntity);
        }
    }
}
