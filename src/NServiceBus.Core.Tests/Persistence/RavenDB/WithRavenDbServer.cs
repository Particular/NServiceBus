namespace NServiceBus.Core.Tests.Persistence.RavenDB
{
    using NServiceBus.Persistence.Raven;
    using NUnit.Framework;
    using Raven.Client.Document;

    public class WithRavenDbServer
    {
        protected DocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            var config = Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName("UnitTests")
                .DefaultBuilder();

            Initialize(config);
          
            store = config.Builder.Build<StoreAccessor>().Store as DocumentStore;
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            store.Dispose();
        }

        protected virtual void Initialize(Configure config)
        {
            config.RavenPersistence();
        }
    }
}
