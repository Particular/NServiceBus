namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Janitor;
    using SimpleJson;

    [SkipWeaving]
    class SagaStorageFile : IDisposable
    {
        SagaStorageFile(FileStream fileStream)
        {
            this.fileStream = fileStream;
            streamWriter = new StreamWriter(fileStream, Encoding.Unicode);
            streamReader = new StreamReader(fileStream, Encoding.Unicode);
        }

        public void Dispose()
        {
            streamWriter.Close();
            streamReader.Close();

            if (isCompleted)
            {
                File.Delete(fileStream.Name);
            }

            fileStream = null;
        }

        public static Task<SagaStorageFile> Open(Guid sagaId, SagaManifest manifest, TimeSpan? maxRetry)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                return noSagaFoundResult;
            }

            return OpenWithRetryOnConcurrency(filePath, FileMode.Open, maxRetry);
        }

        public static Task<SagaStorageFile> Create(Guid sagaId, SagaManifest manifest, TimeSpan? maxRetry)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return OpenWithRetryOnConcurrency(filePath, FileMode.CreateNew, maxRetry);
        }

        static async Task<SagaStorageFile> OpenWithRetryOnConcurrency(string filePath, FileMode fileAccess, TimeSpan? maxRetry)
        {
            var start = DateTimeOffset.UtcNow;

            while (true)
            {
                try
                {
                    return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous));
                }
                catch (IOException)
                {
                    if (!maxRetry.HasValue || DateTimeOffset.UtcNow > (start + maxRetry))
                    {
                        throw;
                    }

                    // give the other task some time to complete the saga to avoid retrying to much
                    await Task.Delay(100)
                        .ConfigureAwait(false);
                }
            }
        }

        public Task Write(IContainSagaData sagaData)
        {
            fileStream.Position = 0;
            var json = SimpleJson.SerializeObject(sagaData, EnumAwareStrategy.Instance);
            return streamWriter.WriteAsync(json);
        }

        public Task MarkAsCompleted()
        {
            isCompleted = true;
            return Task.CompletedTask;
        }

        public async Task<TSagaData> Read<TSagaData>() where TSagaData : class, IContainSagaData
        {
            var json = await streamReader.ReadToEndAsync().ConfigureAwait(false);
            return SimpleJson.DeserializeObject<TSagaData>(json, EnumAwareStrategy.Instance);
        }

        FileStream fileStream;
        bool isCompleted;
        StreamWriter streamWriter;
        StreamReader streamReader;

        const int DefaultBufferSize = 4096;
        static Task<SagaStorageFile> noSagaFoundResult = Task.FromResult<SagaStorageFile>(null);
    }
}