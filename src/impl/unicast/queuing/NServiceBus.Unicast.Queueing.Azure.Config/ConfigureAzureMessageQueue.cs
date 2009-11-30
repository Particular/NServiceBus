using System;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Queueing.Azure.Config
{
    public static class ConfigureAzureMessageQueue
    {
        public static void AzureMessageQueue(this Configure config)
        {
            CloudQueueClient queueClient;

            var configSection = Configure.GetConfigSection<AzureQueueConfig>();

            if (configSection != null)
            {
                queueClient = CloudStorageAccount.Parse(configSection.ConnectionString)
                                        .CreateCloudQueueClient();
            }
            else
            {
                queueClient = CloudStorageAccount.DevelopmentStorageAccount.CreateCloudQueueClient();
            }

            config.Configurer.RegisterSingleton<CloudQueueClient>(queueClient);
       
            config.Configurer.ConfigureComponent<AzureMessageQueue>(ComponentCallModelEnum.Singleton);
        }
    }
}