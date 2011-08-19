using System.Collections.Generic;
using System.Linq;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Hosting
{
    internal class DynamicEndpointLoader
    {
        private readonly CloudBlobClient client;

        public DynamicEndpointLoader()
        {
            var storageAccount = CloudStorageAccount.Parse("UseDevelopmentStorage=true");
            client = storageAccount.CreateCloudBlobClient();
        }

        public IEnumerable<EndpointToHost> LoadEndpoints()
        {
            var blobContainer = client.GetContainerReference("endpoints");
            return from b in blobContainer.ListBlobs()
                   where b.Uri.AbsolutePath.EndsWith(".zip")
                   select new EndpointToHost((CloudBlockBlob)b) ;
        }
    }
}