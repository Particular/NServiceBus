namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using DataBus;
    using Logging;

    class FileShareDataBusImplementation : IDataBus
    {
        public FileShareDataBusImplementation(string basePath)
        {
            this.basePath = basePath;
        }

        public TimeSpan MaxMessageTimeToLive { get; set; }

        public Task<Stream> Get(string key)
        {
            var filePath = Path.Combine(basePath, key);

            logger.DebugFormat("Opening stream from '{0}'.", filePath);

            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, true);
            return Task.FromResult((Stream)fileStream);
        }

        public async Task<string> Put(Stream stream, TimeSpan timeToBeReceived)
        {
            var key = GenerateKey(timeToBeReceived);

            var filePath = Path.Combine(basePath, key);

            Directory.CreateDirectory(Path.GetDirectoryName(filePath));

            using (var output = new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.Read, 4096, FileOptions.Asynchronous))
            {
                const int bufferSize = 32 * 1024;
                await stream.CopyToAsync(output, bufferSize).ConfigureAwait(false);
            }

            logger.DebugFormat("Saved stream to '{0}'.", filePath);

            return key;
        }

        public Task Start()
        {
            logger.Info("File share data bus started. Location: " + basePath);
            //TODO: Implement a clean up thread
            return TaskEx.CompletedTask;
        }

        string GenerateKey(TimeSpan timeToBeReceived)
        {
            if (timeToBeReceived > MaxMessageTimeToLive)
            {
                timeToBeReceived = MaxMessageTimeToLive;
            }

            var keepMessageUntil = DateTime.MaxValue;

            if (timeToBeReceived < TimeSpan.MaxValue)
            {
                keepMessageUntil = DateTime.Now + timeToBeReceived;
            }

            return Path.Combine(keepMessageUntil.ToString("yyyy-MM-dd_HH"), Guid.NewGuid().ToString());
        }

        string basePath;
        static ILog logger = LogManager.GetLogger<FileShareDataBusImplementation>();
    }
}