using System;
using Microsoft.Samples.ServiceHosting.StorageClient;
using NServiceBus.Config;
using NServiceBus.ObjectBuilder;

namespace NServiceBus.Unicast.Queueing.Azure.Config
{
    public static class ConfigureAzureMessageQueue
    {
        public static void AzureMessageQueue(this Configure config)
        {
            var storageAccountInfo = new StorageAccountInfo( new Uri("http://127.0.0.1:10001"), 
                                                         true, 
                                                         "devstoreaccount1",
                                                         "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==");

            var cfg = Configure.GetConfigSection<AzureQueueConfig>();

            if (cfg != null)
            {
                storageAccountInfo.AccountName = cfg.AccountName;
                storageAccountInfo.Base64Key = cfg.Base64Key;
                storageAccountInfo.BaseUri  = new Uri( cfg.BaseUri);
                storageAccountInfo.UsePathStyleUris = cfg.UsePathStyleUris;
            }

            config.Configurer.RegisterSingleton<QueueStorage>(QueueStorage.Create(storageAccountInfo));
       
            config.Configurer.ConfigureComponent<AzureMessageQueue>(ComponentCallModelEnum.Singleton);
        }
    }
}