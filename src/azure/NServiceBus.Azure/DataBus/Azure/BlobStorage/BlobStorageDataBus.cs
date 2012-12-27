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
    using Microsoft.WindowsAzure.StorageClient;
    using Microsoft.WindowsAzure.StorageClient.Protocol;

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
            blob.Attributes.Metadata["ValidUntil"] = (DateTime.Now + timeToBeReceived).ToString();
            UploadBlobInParallel(blob, stream);
            return key;
        }

        public void Start()
        {
            ServicePointManager.DefaultConnectionLimit = NumberOfIOThreads;
            container.CreateIfNotExist();
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
                    DateTime.TryParse(blockBlob.Attributes.Metadata["ValidUntil"], out validUntil);
                    if (validUntil == default(DateTime) || validUntil < DateTime.Now)
                        blockBlob.DeleteIfExists();
                }
            }
            catch (StorageServerException ex) // prevent azure hickups from hurting us.
            {
                logger.Warn(ex.Message);
            }
        }

        private void UploadBlobInParallel(CloudBlockBlob blob, Stream stream)
        {
            var blocksToUpload = new Queue<Block>();
            var order = new List<string>();
            CalculateBlocks(blocksToUpload, (int)stream.Length, order);

            ExecuteInParallel(() => AsLongAsThereAre(blocksToUpload, block =>
            {
                try
                {
                    var buffer = new byte[BlockSize];
                    lock (stream)
                    {
                        stream.Position = block.Offset;
                        stream.Read(buffer, 0, block.Length);
                    }
                    using (var memoryStream = new MemoryStream(buffer, 0, block.Length))
                    {
                        blob.PutBlock(block.Name, memoryStream, null);
                    }
                }
                catch
                {
                    if (++block.Attempt > MaxRetries) throw;

                    lock (blocksToUpload) blocksToUpload.Enqueue(block);
                }
            }));

            Commit(blob, order);
            
        }

        private void Commit(CloudBlockBlob blob, List<string> originalOrder)
        {
            var commitAttempt = 0;
            while (commitAttempt <= MaxRetries)
            {
                try
                {
                    blob.PutBlockList(originalOrder);
                    break;
                }
                catch (Exception)
                {
                    commitAttempt++;
                    if (commitAttempt > MaxRetries) throw;
                }
            }
        }

        private void DownloadBlobInParallel(CloudBlob blob, Stream stream)
        {
            blob.FetchAttributes();
            var order = new List<string>();
            var blocksToDownload = new Queue<Block>();
            CalculateBlocks(blocksToDownload, (int)blob.Properties.Length, order);
            ExecuteInParallel(() => AsLongAsThereAre(blocksToDownload, block =>
            {
                var s = DownloadBlockFromBlob(blob, block, blocksToDownload); if (s == null) return;
                var buffer = new byte[BlockSize];
                ExtractBytesFromBlockIntoBuffer(buffer, s, block);
                lock (stream)
                {
                    stream.Position = block.Offset;
                    stream.Write(buffer, 0,block.Length);
                }
            }));
            stream.Seek(0, SeekOrigin.Begin);
        }

        private void CalculateBlocks(Queue<Block> blocksToUpload, int blobLength, ICollection<string> order)
        {
            var offset = 0;
            while (blobLength > 0)
            {
                var blockLength = Math.Min(BlockSize, blobLength);
                var block = new Block { Offset = offset, Length = blockLength };
                blocksToUpload.Enqueue(block);
                order.Add(block.Name);
                offset += blockLength;
                blobLength -= blockLength;
            }
        }

        private void ExecuteInParallel(Action action)
        {
            var threads = new List<Thread>();
            for (var i = 0; i < NumberOfIOThreads; i++)
            {
                var t = new Thread(new ThreadStart(action));
                t.Start();
                threads.Add(t);
            }
            foreach (var t in threads) t.Join();
        }

        private static void AsLongAsThereAre(Queue<Block> blocks, Action<Block> action)
        {
            while (true)
            {
                Block block;
                lock (blocks)
                {
                    if (blocks.Count == 0)
                        break;

                    block = blocks.Dequeue();
                }

                action(block);
            }
        }

        private static void ExtractBytesFromBlockIntoBuffer(byte[] buffer, Stream s, Block block)
        {
            var offsetInBlock = 0;
            var remaining = block.Length;
            while (remaining > 0)
            {
                var read = s.Read(buffer, offsetInBlock, remaining);
                offsetInBlock += read;
                remaining -= read;
            }
        }

        private Stream DownloadBlockFromBlob(CloudBlob blob, Block block, Queue<Block> blocksToDownload)
        {
            try
            {
                var blobGetRequest = BlobRequest.Get(blob.Uri, 60, null, null);
                blobGetRequest.Headers.Add("x-ms-range", string.Format(CultureInfo.InvariantCulture, "bytes={0}-{1}", block.Offset, block.Offset + block.Length - 1));
                var credentials = blob.ServiceClient.Credentials;
                credentials.SignRequest(blobGetRequest);
                var response = blobGetRequest.GetResponse() as HttpWebResponse;
                return response != null ? response.GetResponseStream() : null;
            }
            catch
            {
                if (++block.Attempt > MaxRetries) throw;

                lock (blocksToDownload) blocksToDownload.Enqueue(block);
            }
            return null;
        }
    }
}
