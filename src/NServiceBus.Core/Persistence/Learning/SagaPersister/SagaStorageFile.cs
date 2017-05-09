namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Janitor;
    using Newtonsoft.Json;

    [SkipWeaving]
    class SagaStorageFile : IDisposable
    {
        SagaStorageFile(FileStream fileStream, SagaManifest manifest)
        {
            this.fileStream = fileStream;
            this.manifest = manifest;
            textWriter = new StreamWriter(fileStream, Encoding.Unicode);
            jsonWriter = new JsonTextWriter(textWriter)
            {
                CloseOutput = true
            };
            textReader = new StreamReader(fileStream, Encoding.Unicode);
            jsonReader = new JsonTextReader(textReader)
            {
                CloseInput = true
            };

            lastModificationSeenAt = File.GetLastWriteTimeUtc(fileStream.Name);
        }

        public void Dispose()
        {
            jsonWriter.Close();
            textWriter.Dispose();
            jsonReader.Close();
            textReader.Dispose();
            fileStream.Dispose();

            if (isCompleted)
            {
                File.Delete(fileStream.Name);
            }

            fileStream = null;
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

        public Task Write(IContainSagaData sagaData)
        {
            fileStream.Position = 0;

            return WriteWithinLock((stream, data, meta) =>
            {
                serializer.Serialize(jsonWriter, data);
                return TaskEx.CompletedTask;
            }, fileStream, lastModificationSeenAt, sagaData, manifest);
        }

        public async Task MarkAsCompleted()
        {
            await WriteWithinLock((stream, data, meta) => stream.WriteAsync(EmptyBytes, 0, 0), fileStream, lastModificationSeenAt)
                .ConfigureAwait(false);

            isCompleted = true;
        }

        public object Read()
        {
            return serializer.Deserialize(jsonReader, manifest.SagaEntityType);
        }

        static async Task WriteWithinLock(Func<FileStream, IContainSagaData, SagaManifest, Task> action, FileStream stream, DateTime lastModificationSeenAt, IContainSagaData sagaData = null, SagaManifest manifest = null)
        {
            var targetPath = stream.Name;
            var lockFilePath = Path.ChangeExtension(targetPath, ".lock");
            // will blow up in case of concurrency
            using (new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                ThrowWhenModifiedSinceLastRead(lastModificationSeenAt, targetPath);

                await action(stream, sagaData, manifest)
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
                throw new LearningSagaPersisterConcurrencyException();
            }
        }

        SagaManifest manifest;
        FileStream fileStream;
        DateTime lastModificationSeenAt;
        bool isCompleted;
        Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
        StreamWriter textWriter;
        JsonTextWriter jsonWriter;
        StreamReader textReader;
        JsonTextReader jsonReader;

        const int DefaultBufferSize = 4096;
        static byte[] EmptyBytes = new byte[0];
    }
}