using NServiceBus.Config.ConfigurationSource;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queuing.Azure.Tests
{
    using Microsoft.WindowsAzure.Storage.Queue;

    [TestFixture]
    [Category("Azure")]
    public class When_configuring_the_azure_queue
    {
        private Configure configure;

        [SetUp]
        public void Setup()
        {
            configure = Configure.With()
                .DefineEndpointName("Test")
                .DefaultBuilder();
        }
        [Test]
        public void The_storage_should_default_to_dev_settings_if_no_config_section_is_found()
        {
            configure
                .CustomConfigurationSource(new NullSource())
                .AzureMessageQueue();

            Assert.AreEqual(Configure.Instance.Builder.Build<CloudQueueClient>().Credentials.AccountName,"devstoreaccount1");

        }


        [Test]
        public void Storage_setting_should_be_read_from_configuration_source()
        {
            configure
                .AzureMessageQueue();

            var storage = Configure.Instance.Builder.Build<CloudQueueClient>();

            Assert.AreEqual(storage.Credentials.AccountName,"myaccount");
        }

        [Test]
        public void The_azurequeue_should_not_be_singleton()
        {
            configure
             .AzureMessageQueue();

            Assert.AreNotEqual(Configure.Instance.Builder.Build<AzureMessageQueueReceiver>(), Configure.Instance.Builder.Build<AzureMessageQueueReceiver>());
        }
    }

    public class NullSource : IConfigurationSource
    {
        T IConfigurationSource.GetConfiguration<T>()
        {
            return null;
        }
    }
} 