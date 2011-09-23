using System;
using System.IO;
using Ionic.Zip;
using Microsoft.WindowsAzure.StorageClient;

namespace NServiceBus.Hosting
{
    public class EndpointToHost
    {
        private readonly CloudBlockBlob blob;

        public EndpointToHost(CloudBlockBlob blob)
        {
            this.blob = blob;
            this.blob.FetchAttributes();
            EndpointName = Path.GetFileNameWithoutExtension(blob.Uri.AbsolutePath);
            LastUpdated = blob.Properties.LastModifiedUtc;
        }

        public string EndpointName { get; private set; }

        public string EntryPoint { get; set; }

        public int ProcessId { get; set; }

        public DateTime LastUpdated { get; set; }


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