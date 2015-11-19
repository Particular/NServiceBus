namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = new InMemorySagaPersister();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>("propertyNotFound", "someValue", new ContextBagImpl());
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = new InMemorySagaPersister();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>(Guid.Empty, new ContextBagImpl());
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
            var persister = new InMemorySagaPersister();
            await persister.Save(simpleSagaEntity, SagaMetadataHelper.GetMetadata<SimpleSagaEntitySaga>(simpleSagaEntity), new ContextBagImpl());

            var anotherSagaEntity = await persister.Get<AnotherSimpleSagaEntity>(id, new ContextBagImpl());
            Assert.IsNull(anotherSagaEntity);
        }

        public class AnotherSimpleSagaEntity : ContainSagaData
        {
        }
    }
}