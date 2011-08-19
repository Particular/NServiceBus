using System.IO;
using Ionic.Zip;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Hosting
{
    internal class EndpointToHost
    {
        private readonly CloudBlockBlob blob;

        public EndpointToHost(CloudBlockBlob blob)
        {
            this.blob = blob;
            EndpointName = Path.GetFileNameWithoutExtension(blob.Uri.AbsolutePath);
        }

        public string EndpointName { get; private set; }
        
        public string ExtractTo(string rootPath)
        {
            var localDirectory = Path.Combine(rootPath, EndpointName);
            var localFileName = Path.Combine(rootPath, Path.GetFileName(blob.Uri.AbsolutePath));
            
            blob.DownloadToFile(localFileName);

            using(var zip = new ZipFile(localFileName))
            {
                zip.ExtractAll(localDirectory, ExtractExistingFileAction.OverwriteSilently);
            }

            return localDirectory;
        }
    }
}