using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;

namespace NServiceBus.Hosting
{
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

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
            blobContainer.CreateIfNotExists();

            return from b in blobContainer.ListBlobs()
                    where b.Uri.AbsolutePath.EndsWith(".zip")
                    select new EndpointToHost((CloudBlockBlob)b) ;
        }
    }
}