namespace NServiceBus.Persistence.ComponentTests
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_saga_not_found_return_default : SagaPersisterTests
    {
        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_property()
        {
            var persister = configuration.SagaStorage;
            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                var simpleSageEntity = await persister.Get<SimpleSagaEntity>("propertyNotFound", "someValue", session, readContextBag);
                Assert.IsNull(simpleSageEntity);
            }
        }

        [Test]
        public async Task Should_return_default_when_using_finding_saga_with_id()
        {
            var persister = configuration.SagaStorage;
            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                var simpleSageEntity = await persister.Get<SimpleSagaEntity>(Guid.Empty, session, readContextBag);
                Assert.IsNull(simpleSageEntity);
            }
        }

        public class AnotherSimpleSagaEntity : ContainSagaData
        {
        }
    }
}