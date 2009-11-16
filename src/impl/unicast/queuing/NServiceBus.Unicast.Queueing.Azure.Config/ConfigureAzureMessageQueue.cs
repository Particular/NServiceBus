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

            var cfg = Configure.GetConfigSection<AzureQueueConfig>();

            if (cfg != null)
            {
                
                var account =
                    new CloudStorageAccount(new StorageCredentialsAccountAndKey(cfg.AccountName, cfg.Base64Key),cfg.UseHttps);

                queueClient = account.CreateCloudQueueClient();
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