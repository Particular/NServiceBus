using System.Configuration;
using NServiceBus.Config;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Host.Internal;
using NServiceBus.Unicast.Subscriptions.Msmq;
using NServiceBus.Unicast.Subscriptions.NHibernate.Config;
using NUnit.Framework;
using NBehave.Spec.NUnit;
using Rhino.Mocks;

namespace NServiceBus.Host.Tests
{
    [TestFixture]
    public class When_configuring_an_endpoint_as_a_publisher
    {
        [Test]
        public void Db_subscription_storage_should_be_used_if_config_section_is_found()
        {
            var configSource = MockRepository.GenerateStub<IConfigurationSource>();

            configSource.Stub(x => x.GetConfiguration<DBSubscriptionStorageConfig>())
                .Return(ConfigurationManager.GetSection("DBSubscriptionStorageConfig_with_no_nhproperties") as DBSubscriptionStorageConfig);

            ServerEndpointConfigWithCustomConfigSource.ConfigurationSource = configSource;

            var configure = Util.Init<ServerEndpointConfigWithCustomConfigSource>();

            configure.Builder.Build<Unicast.Subscriptions.NHibernate.SubscriptionStorage>().ShouldNotBeNull();
        }
    }

}