using System;
using NUnit.Framework;

namespace NServiceBus.Unicast.Subscriptions.NHibernate.Tests.Config
{
    [TestFixture]
    public class When_configuring_the_subscription_storage
    {
        private Configure config;

        [SetUp]
        public void SetUp()
        {
            config = Configure.With(new Type[] { })
                                .DefineEndpointName("Foo")
                                .DefaultBuilder()
                                .UseNHibernateSubscriptionPersister();
        }

        [Test]
        public void The_session_provider_should_be_registered_as_singleton()
        {
            var sessionSource = config.Builder.Build<ISubscriptionStorageSessionProvider>();

            Assert.AreSame(sessionSource, config.Builder.Build<ISubscriptionStorageSessionProvider>());
        }


        [Test]
        public void The_storage_should_be_registered_as_singlecall()
        {

            var subscriptionStorage = config.Builder.Build<SubscriptionStorage>();

            Assert.AreNotSame(subscriptionStorage, config.Builder.Build<SubscriptionStorage>());
        }
    }
}