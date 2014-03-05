namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;

    [TestFixture]
    class When_saga_not_found_return_default
    {

        [Test]
        public void Should_return_default_when_using_finding_saga_with_property()
        {
            var p = new InMemorySagaPersister();
            var simpleSageEntity = p.Get<SimpleSageEntity>("propertyNotFound", null);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_id()
        {
            var p = new InMemorySagaPersister();
            var simpleSageEntity = p.Get<SimpleSageEntity>(Guid.Empty);
            Assert.IsNull(simpleSageEntity);
        }

        [Test]
        public void Should_return_default_when_using_finding_saga_with_id_of_another_type()
        {
            var p = new InMemorySagaPersister();
            var id = Guid.NewGuid();
            var simpleSagaEntity = new SimpleSageEntity
            {
                Id = id,
                OrderSource = "CA"
            };
            p.Save(simpleSagaEntity);

            var anotherSagaEntity = p.Get<AnotherSimpleSageEntity>(id);
            Assert.IsNull(anotherSagaEntity);
        }
    }
}
