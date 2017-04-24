namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Janitor;

    [SkipWeaving]
    class SagaStorageFile
    {
        SagaStorageFile(FileStream fileStream, SagaManifest manifest)
        {
            this.fileStream = fileStream;
            this.manifest = manifest;

            lastModificationSeenAt = File.GetLastWriteTimeUtc(fileStream.Name);
        }

        public static bool TryOpen(Guid sagaId, SagaManifest manifest, out SagaStorageFile sagaStorageFile)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                sagaStorageFile = null;
                return false;
            }

            sagaStorageFile = new SagaStorageFile(new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite, DefaultBufferSize, FileOptions.Asynchronous), manifest);

            return true;
        }

        public static SagaStorageFile Create(Guid sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return new SagaStorageFile(new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.ReadWrite, DefaultBufferSize, FileOptions.Asynchronous), manifest);
        }

        public void Dispose()
        {
            fileStream.Close();
            fileStream.Dispose();

            if (isCompleted)
            {
                File.Delete(fileStream.Name);
            }

            fileStream = null;
        }

        public Task Write(IContainSagaData sagaData)
        {
            ThrowWhenModifiedSinceLastRead(lastModificationSeenAt, fileStream.Name);

            fileStream.Position = 0;
            manifest.Serializer.WriteObject(fileStream, sagaData);
            return fileStream.FlushAsync();
        }

        public async Task MarkAsCompleted()
        {
            ThrowWhenModifiedSinceLastRead(lastModificationSeenAt, fileStream.Name);

            await fileStream.WriteAsync(new byte[0], 0, 0)
                .ConfigureAwait(false);
            await fileStream.FlushAsync()
                .ConfigureAwait(false);

            isCompleted = true;
        }

        public object Read()
        {
            return manifest.Serializer.ReadObject(fileStream);
        }

        static void ThrowWhenModifiedSinceLastRead(DateTime lastModificationSeenAt, string filePath)
        {
            var lastWriteTime = File.GetLastWriteTimeUtc(filePath);
            if (lastWriteTime != lastModificationSeenAt || !File.Exists(filePath))
            {
                throw new ConcurrencyException();
            }
        }

        readonly SagaManifest manifest;
        FileStream fileStream;
        DateTime lastModificationSeenAt;
        bool isCompleted;
        const int DefaultBufferSize = 4096;
    }
}