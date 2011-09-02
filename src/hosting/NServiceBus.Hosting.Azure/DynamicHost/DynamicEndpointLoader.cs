using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.ServiceRuntime;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointLoader
    {
        private readonly CloudBlobClient client;

        public DynamicEndpointLoader()
        {
            string connectionString;
            try
            {
                connectionString = RoleEnvironment.GetConfigurationSettingValue("NServiceBus.Host.ConnectionString");
            }
            catch (Exception)
            {
                connectionString = "UseDevelopmentStorage=true";
            }

            var storageAccount = CloudStorageAccount.Parse(connectionString);
            client = storageAccount.CreateCloudBlobClient();
        }

        public IEnumerable<EndpointToHost> LoadEndpoints()
        {
            string container;
            try
            {
                container = RoleEnvironment.GetConfigurationSettingValue("NServiceBus.Host.Container");
            }
            catch (Exception)
            {
                container = "endpoints";
            }

            var blobContainer = client.GetContainerReference(container);
            return from b in blobContainer.ListBlobs()
                   where b.Uri.AbsolutePath.EndsWith(".zip")
                   select new EndpointToHost((CloudBlockBlob)b) ;
        }
    }
}