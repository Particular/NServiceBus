using System.Configuration;
using NServiceBus.Host.Internal;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Subscriptions.NHibernate.Config;
using NUnit.Framework;
using NBehave.Spec.NUnit;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_configuring_an_endpoint_as_a_publisher
    {
        private Configure busConfig;

        [SetUp]
        public void SetUp()
        {
            busConfig = new ConfigurationBuilder(new ServerEndpointConfig(), typeof(ServerEndpoint))
                .Build();

        }
        [Test]
        public void Msmq_should_be_default_subscription_storage()
        {
            busConfig.Builder.Build<MsmqSubscriptionStorage>().ShouldNotBeNull();
        }

        [Test]
        public void The_user_can_request_the_nhibernate_subscription_storage_to_be_used()
        {
            new ConfigurationBuilder(new NHibernateSubscriptionStorageEndpointConfig(), typeof(ServerEndpoint))
                .Build()
                .Builder.Build<Unicast.Subscriptions.NHibernate.SubscriptionStorage>()
                    .ShouldNotBeNull();
        }



    }

    public class NHibernateSubscriptionStorageEndpointConfig : IConfigureThisEndpoint,As.aPublisher,ISpecify.ToUseNHibernateSubscriptionStorage 
    {
    }
}