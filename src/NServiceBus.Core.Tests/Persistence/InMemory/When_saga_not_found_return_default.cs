namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = new InMemorySagaPersister();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>("propertyNotFound", "someValue", new InMemorySynchronizedStorageSession(), new ContextBag());
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = new InMemorySagaPersister();
            var simpleSageEntity = await persister.Get<SimpleSagaEntity>(Guid.Empty, new InMemorySynchronizedStorageSession(), new ContextBag());
            Assert.IsNull(simpleSageEntity);
        }

        [Ignore("It should not return null when the type mismatches.")]
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
            var session = new InMemorySynchronizedStorageSession();
            await persister.Save(simpleSagaEntity, SagaMetadataHelper.GetMetadata<SimpleSagaEntitySaga>(simpleSagaEntity), session, new ContextBag());
            await session.CompleteAsync();
            var anotherSagaEntity = await persister.Get<AnotherSimpleSagaEntity>(id, new InMemorySynchronizedStorageSession(),  new ContextBag());
            Assert.IsNull(anotherSagaEntity);
        }

        public class AnotherSimpleSagaEntity : ContainSagaData
        {
        }
    }
}