namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
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

        public static Task<SagaStorageFile> Open(Guid sagaId, SagaManifest manifest, CancellationToken cancellationToken = default)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                return noSagaFoundResult;
            }

            return OpenWithRetryOnConcurrency(filePath, FileMode.Open, cancellationToken);
        }

        public static Task<SagaStorageFile> Create(Guid sagaId, SagaManifest manifest, CancellationToken cancellationToken = default)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return OpenWithRetryOnConcurrency(filePath, FileMode.CreateNew, cancellationToken);
        }

        static async Task<SagaStorageFile> OpenWithRetryOnConcurrency(string filePath, FileMode fileAccess, CancellationToken cancellationToken = default)
        {
            var numRetries = 0;

            while (true)
            {
                try
                {
                    return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous));
                }
                catch (IOException)
                {
                    numRetries++;

                    if (numRetries > 4) // Given the 100ms delay below, we wait roughly 500 ms for the file to become unlocked
                    {
                        throw;
                    }

                    // Give the other task some time to complete the saga to avoid retrying too much
                    await Task.Delay(100, cancellationToken)
                        .ConfigureAwait(false);
                }
            }
        }

        public Task Write(IContainSagaData sagaData, CancellationToken cancellationToken = default)
        {
            // The token isn't currently required but later, a method in a descendant call stack may get a token overload.
            // When that happens, we want CA2016 to tell us to forward the token, so we want to keep the parameter.
            // This line makes the parameter "required".
            cancellationToken.ThrowIfCancellationRequested();

            fileStream.Position = 0;
            var json = SimpleJson.SerializeObject(sagaData, EnumAwareStrategy.Instance);
            return streamWriter.WriteAsync(json);
        }

        public void MarkAsCompleted()
        {
            isCompleted = true;
        }

        public async Task<TSagaData> Read<TSagaData>(CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
        {
            // The token isn't currently required but later, a method in a descendant call stack may get a token overload.
            // When that happens, we want CA2016 to tell us to forward the token, so we want to keep the parameter.
            // This line makes the parameter "required".
            cancellationToken.ThrowIfCancellationRequested();

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