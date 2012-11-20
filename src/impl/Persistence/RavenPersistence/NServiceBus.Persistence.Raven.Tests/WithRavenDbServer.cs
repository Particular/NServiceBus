namespace NServiceBus.Persistence.Raven.Tests
{
    using NUnit.Framework;
    using global::Raven.Client;
    using global::Raven.Client.Document;

    public class WithRavenDbServer
    {
        protected DocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            ConfigureRavenPersistence.AutoCreateDatabase = false;

            var config = Configure.With(new[] { GetType().Assembly })
                .DefineEndpointName("UnitTests")
                .DefaultBuilder();

            Configure.DefineEndpointVersionRetriever = () => "1.0.0.0";

            Initialize(config);
          
            store = config.Builder.Build<IDocumentStore>() as DocumentStore;
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
