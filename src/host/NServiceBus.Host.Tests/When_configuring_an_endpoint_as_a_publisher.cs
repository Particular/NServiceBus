using System.Configuration;
using NServiceBus.Host.Internal;
using NServiceBus.Unicast.Subscriptions.Msmq;
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
            busConfig = new ConfigurationBuilder()
                .BuildConfigurationFrom(new ServerEndpointConfig(), typeof(ServerEndpoint));

        }
        [Test, ExpectedException(typeof(ConfigurationErrorsException))]
        public void The_endpoint_must_be_a_server_as_well()
        {
            new ConfigurationBuilder().BuildConfigurationFrom(new PublisherNotSpecifiedAsServer(), typeof(ServerEndpoint));
        }
        [Test]
        public void Msmq_should_be_default_subscription_storage()
        {
            busConfig.Builder.Build<MsmqSubscriptionStorage>().ShouldNotBeNull();
        }

        
    }

    public class PublisherNotSpecifiedAsServer : IConfigureThisEndpoint,As.aPublisher  
    {
    }
}