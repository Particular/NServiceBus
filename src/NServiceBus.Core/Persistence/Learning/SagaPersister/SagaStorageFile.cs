namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Janitor;

    [SkipWeaving]
    class SagaStorageFile : IDisposable
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
            fileStream.Position = 0;

            return WriteWithinLock((stream, state) =>
            {
                var serializer = state.Manifest.Serializer;
                var data = state.SagaData;
                serializer.WriteObject(stream, data);
                return TaskEx.CompletedTask;
            }, fileStream, lastModificationSeenAt, new WriteState(sagaData, manifest));
        }

        public async Task MarkAsCompleted()
        {
            await WriteWithinLock((stream, state) => stream.WriteAsync(EmptyBytes, 0, 0), fileStream, lastModificationSeenAt)
                .ConfigureAwait(false);

            isCompleted = true;
        }

        public object Read()
        {
            return manifest.Serializer.ReadObject(fileStream);
        }

        static Task WriteWithinLock(Func<FileStream, object, Task> action, FileStream stream, DateTime lastModificationSeenAt)
        {
            return WriteWithinLock<object>(action, stream, lastModificationSeenAt);
        }

        static async Task WriteWithinLock<TState>(Func<FileStream, TState, Task> action, FileStream stream, DateTime lastModificationSeenAt, TState state = default(TState))
        {
            var targetPath = stream.Name;
            var lockFilePath = Path.ChangeExtension(targetPath, ".lock");
            // will blow up in case of concurrency
            using (new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                ThrowWhenModifiedSinceLastRead(lastModificationSeenAt, targetPath);

                await action(stream, state)
                    .ConfigureAwait(false);
                await stream.FlushAsync()
                    .ConfigureAwait(false);
            }
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
        static byte[] EmptyBytes = new byte[0];

        struct WriteState
        {
            public WriteState(IContainSagaData sagaData, SagaManifest manifest)
            {
                SagaData = sagaData;
                Manifest = manifest;
            }

            public readonly IContainSagaData SagaData;
            public readonly SagaManifest Manifest;
        }
    }
}