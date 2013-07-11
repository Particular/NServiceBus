using NServiceBus.Logging;

namespace NServiceBus.DataBus.Azure.BlobStorage
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Blob.Protocol;

    public class BlobStorageDataBus : IDataBus
    {
        private readonly ILog logger = LogManager.GetLogger(typeof(IDataBus));
        private readonly CloudBlobContainer container;
        private readonly Timer timer;
        
        public int MaxRetries { get; set; }
        public int NumberOfIOThreads { get; set; }
        public string BasePath { get; set; }
        public int BlockSize { get; set; }

        public BlobStorageDataBus(CloudBlobContainer container)
        {
            this.container = container;
            timer = new Timer(o => DeleteExpiredBlobs());
        }

        public Stream Get(string key)
        {
            var stream = new MemoryStream();
            var blob = container.GetBlockBlobReference(Path.Combine(BasePath, key));
            DownloadBlobInParallel(blob, stream);
            return stream;
        }

        

        public string Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = Guid.NewGuid().ToString();
            var blob = container.GetBlockBlobReference(Path.Combine(BasePath, key));
            blob.Metadata["ValidUntil"] = (DateTime.Now + timeToBeReceived).ToString();
            UploadBlobInParallel(blob, stream);
            return key;
        }

        public void Start()
        {
            ServicePointManager.DefaultConnectionLimit = NumberOfIOThreads;
            container.CreateIfNotExists();
            timer.Change(0, 300000);
            logger.Info("Blob storage data bus started. Location: " + BasePath);
        }

        public void Dispose()
        {
            timer.Dispose();

            DeleteExpiredBlobs();

            logger.Info("Blob storage data bus stopped");
        }

        private void DeleteExpiredBlobs()
        {
            try
            {
                var blobs = container.ListBlobs();
                foreach (var blockBlob in blobs.Select(blob => blob as CloudBlockBlob))
                {
                    if (blockBlob == null) continue;

                    blockBlob.FetchAttributes();
                    DateTime validUntil;
                    DateTime.TryParse(blockBlob.Metadata["ValidUntil"], out validUntil);
                    if (validUntil == default(DateTime) || validUntil < DateTime.Now)
                        blockBlob.DeleteIfExists();
                }
            }
            catch (StorageException ex) 
            {
                logger.Warn(ex.Message);
            }
        }

        private void UploadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            try
            {
                blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
                blob.UploadFromStream(stream);
            }
            catch (StorageException ex)
            {
                 logger.Warn(ex.Message);
            }
        }

        private void DownloadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            try
            {
                blob.FetchAttributes();
                blob.ServiceClient.ParallelOperationThreadCount = NumberOfIOThreads;
                blob.DownloadToStream(stream);
                stream.Seek(0, SeekOrigin.Begin);
            }
            catch (StorageException ex) 
            {
                logger.Warn(ex.Message);
            }
        }

    }
}
