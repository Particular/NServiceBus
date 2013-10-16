namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NUnit.Framework;
    using Persistence.InMemory.SagaPersister;
    using Saga;

    [TestFixture]
    class When_saga_not_found_return_default
    {
        [Test]
        public void Should_return_default_when_using_finding_saga_with_property()
        {
            var p = new InMemorySagaPersister() as ISagaPersister;
            var simpleSageEntity = p.Get<SimpleSageEntity>("propertyNotFound", null);
            Assert.AreEqual(simpleSageEntity, default(SimpleSageEntity));
        }
        [Test]
        public void Should_return_default_when_using_finding_saga_with_id()
        {
            var p = new InMemorySagaPersister() as ISagaPersister;
            var simpleSageEntity = p.Get<SimpleSageEntity>(Guid.Empty);
            Assert.AreSame(simpleSageEntity, default(SimpleSageEntity));
        }


        [Test]
        public void Should_return_default_when_using_finding_saga_with_id_of_another_type()
        {
            var p = new InMemorySagaPersister() as ISagaPersister;
            var id = Guid.NewGuid();
            var simpleSagaEntity = new SimpleSageEntity
            {
                Id = id,
                OrderSource = "CA"
            };
            p.Save(simpleSagaEntity);

            var anotherSagaEntity = p.Get<AnotherSimpleSageEntity>(id);
            Assert.AreSame(anotherSagaEntity, default(AnotherSimpleSageEntity));
        }
    }
}
