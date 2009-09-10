using System.Configuration;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;
using NServiceBus.Host.Internal.ProfileHandlers;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NBehave.Spec.NUnit;
using Rhino.Mocks;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_running_in_the_integration_profile
    {
        [Test]
        public void Msmq_subscription_storage_should_be_used()
        {
            var config = Util.Init<ServerEndpointConfig, IntegrationProfileHandler>();

            var subscriptionStorage = config.Builder.Build<MsmqSubscriptionStorage>();

            subscriptionStorage.ShouldNotBeNull();

            //subscriptionStorage.Queue.ShouldStartWith(typeof(ServerEndpointConfig).FullName);

        }

        [Test]
        public void Msmq_subscription_storage_should_pick_queue_name_from_app_config_if_present()
        {
            var configSource = MockRepository.GenerateStub<IConfigurationSource>();

            configSource.Stub(x => x.GetConfiguration<MsmqSubscriptionStorageConfig>())
                .Return(ConfigurationManager.GetSection("MsmqSubscriptionStorageConfig_with_queue_specified") as MsmqSubscriptionStorageConfig);

            ServerEndpointConfigWithCustomConfigSource.ConfigurationSource = configSource;

            var config = Util.Init<ServerEndpointConfigWithCustomConfigSource, IntegrationProfileHandler>();

            var subscriptionStorage = config.Builder.Build<MsmqSubscriptionStorage>();

            subscriptionStorage.ShouldNotBeNull();

            subscriptionStorage.Queue.ShouldEqual("test_queue");

        }

    }
}