using NServiceBus.Persistence.Raven.Config;
using NServiceBus.Unicast.Subscriptions.Raven.Config;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Document;

namespace NServiceBus.Unicast.Subscriptions.Raven.Tests.Config
{
    [TestFixture]
    public class When_configuring_the_raven_saga_persister_with_the_default_settings_and_raven_persistance_has_been_configured : WithRavenDbServer
    {
        RavenSubscriptionStorage subscriptionStorage;
        IDocumentStore store;

        [TestFixtureSetUp]
        public void SetUp()
        {
            using (var server = GetNewServer())
            {
                var config = Configure.With(new[] {GetType().Assembly})
                    .DefaultBuilder()
                    .RavenPersistence();

                store = config.Builder.Build<IDocumentStore>();

                config = config.RavenSubscriptionStorage();

                subscriptionStorage = config.Builder.Build<RavenSubscriptionStorage>();
            }
        }

        [Test]
        public void It_should_configure_raven_to_use_the_configured_raven_persistance()
        {
            var actual = subscriptionStorage.Store as DocumentStore;
            Assert.AreSame(store, actual);
        }

        [Test]
        public void It_should_configure_to_use_the_calling_assembly_name_as_the_endpoint()
        {
            Assert.AreEqual(GetType().Assembly.GetName().Name, subscriptionStorage.Endpoint);
        }
    }
}