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
            jsonWriter = new JsonTextWriter(new StreamWriter(fileStream, Encoding.Unicode))
            {
                CloseOutput = true,
                Formatting = Formatting.Indented
            };
            jsonReader = new JsonTextReader(new StreamReader(fileStream, Encoding.Unicode))
            {
                CloseInput = true
            };

            lastModificationSeenAt = File.GetLastWriteTimeUtc(fileStream.Name);
        }

        public void Dispose()
        {
            jsonWriter.Close();
            jsonReader.Close();

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

            sagaStorageFile = new SagaStorageFile(new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous), manifest);

            return true;
        }

        public static SagaStorageFile Create(Guid sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return new SagaStorageFile(new FileStream(filePath, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous), manifest);
        }

        public Task Write(IContainSagaData sagaData)
        {
            fileStream.Position = 0;

            WriteWithinLock((writer, data) => serializer.Serialize(writer, data), jsonWriter, fileStream.Name, lastModificationSeenAt, sagaData);
            return TaskEx.CompletedTask;
        }

        public Task MarkAsCompleted()
        {
            WriteWithinLock((writer, data) => writer.WriteNull(), jsonWriter, fileStream.Name, lastModificationSeenAt);

            isCompleted = true;
            return TaskEx.CompletedTask;
        }

        public object Read()
        {
            return serializer.Deserialize(jsonReader, manifest.SagaEntityType);
        }

        static void WriteWithinLock(Action<JsonWriter, IContainSagaData> action, JsonWriter writer, string targetPath, DateTime lastModificationSeenAt, IContainSagaData sagaData = null)
        {
            var lockFilePath = Path.ChangeExtension(targetPath, ".lock");
            // will blow up in case of concurrency
            using (new FileStream(lockFilePath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1, FileOptions.DeleteOnClose))
            {
                ThrowWhenModifiedSinceLastRead(lastModificationSeenAt, targetPath);

                action(writer, sagaData);
                writer.Flush();
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
        JsonTextWriter jsonWriter;
        JsonTextReader jsonReader;

        const int DefaultBufferSize = 4096;
        static Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
    }
}