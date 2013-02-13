namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using NUnit.Framework;
    using Raven.Client;

    [TestFixture]
    public class With_using_raven_persistence
    {
        [Test]
        public void Should_not_register_IDocumentStore_into_the_container()
        {
            var config = Configure.With(new[] { GetType().Assembly })
                                  .DefineEndpointName("UnitTests")
                                  .DefaultBuilder()
                                  .RavenPersistence();

            Assert.IsFalse(config.Configurer.HasComponent<IDocumentStore>());
        }
    }
}