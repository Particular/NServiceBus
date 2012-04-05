using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointLoader
    {
        private CloudBlobClient client;
        

        public string ConnectionString { get; set; }
        public string Container { get; set; }

        public IEnumerable<EndpointToHost> LoadEndpoints()
        {
            if (client == null)
            {
                var storageAccount = CloudStorageAccount.Parse(ConnectionString);
                client = storageAccount.CreateCloudBlobClient();
            }

            var blobContainer = client.GetContainerReference(Container);
            blobContainer.CreateIfNotExist();

            return from b in blobContainer.ListBlobs()
                    where b.Uri.AbsolutePath.EndsWith(".zip")
                    select new EndpointToHost((CloudBlockBlob)b) ;
        }
    }
}