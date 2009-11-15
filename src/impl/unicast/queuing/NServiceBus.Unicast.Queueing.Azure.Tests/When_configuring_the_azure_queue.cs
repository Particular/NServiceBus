using System;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NBehave.Spec.NUnit;
using NServiceBus.Config.ConfigurationSource;
using NServiceBus.Unicast.Queueing.Azure.Config;
using NUnit.Framework;

namespace NServiceBus.Unicast.Queueing.Azure.Tests
{
    [TestFixture]
    public class When_configuring_the_azure_queue
    {

        [Test]
        public void The_storage_should_default_to_dev_settings_if_no_config_section_is_found()
        {

            Configure.With()
                .SpringBuilder()
                .CustomConfigurationSource(new NullSource())
                .AzureMessageQueue();

            Configure.Instance.Builder.Build<QueueStorage>().AccountName.ShouldEqual("devstoreaccount1");
        }


        [Test]
        public void The_storage_should_read_setting_from_appconfig()
        {

            Configure.With()
                .SpringBuilder()
                .AzureMessageQueue();

            var storage = Configure.Instance.Builder.Build<QueueStorage>();

            storage.AccountName.ShouldEqual("myaccount");
            storage.BaseUri.ShouldEqual(new Uri("http://queue.core.windows.net"));
            storage.UsePathStyleUris.ShouldBeFalse();
        }

        [Test]
        public void The_azurequeue_should_be_singleton()
        {
            Configure.With()
             .SpringBuilder()
             .AzureMessageQueue();

            Configure.Instance.Builder.Build<AzureMessageQueue>()
                .ShouldBeTheSameAs(Configure.Instance.Builder.Build<AzureMessageQueue>());
        }
    }

    public class NullSource : IConfigurationSource
    {
        public T GetConfiguration<T>() where T : class
        {
            return null;
        }
    }
} 