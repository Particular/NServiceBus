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

        public static Task<SagaStorageFile> Open(Guid sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                return noSagaFoundResult;
            }

            return OpenWithDelayOnConcurrency(manifest, filePath, FileMode.Open);
        }

        public static Task<SagaStorageFile> Create(Guid sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return OpenWithDelayOnConcurrency(manifest, filePath, FileMode.CreateNew);
        }

        static async Task<SagaStorageFile> OpenWithDelayOnConcurrency(SagaManifest manifest, string filePath, FileMode fileAccess)
        {
            try
            {
                return new SagaStorageFile(new FileStream(filePath, fileAccess, FileAccess.ReadWrite, FileShare.None, DefaultBufferSize, FileOptions.Asynchronous), manifest);
            }
            catch (IOException)
            {
                // give the other task some time to complete the saga to avoid retrying to much
                await Task.Delay(100)
                    .ConfigureAwait(false);

                throw;
            }
        }


        public Task Write(IContainSagaData sagaData)
        {
            fileStream.Position = 0;
            serializer.Serialize(jsonWriter, sagaData);

            return TaskEx.CompletedTask;
        }

        public Task MarkAsCompleted()
        {
            isCompleted = true;
            return TaskEx.CompletedTask;
        }

        public object Read()
        {
            return serializer.Deserialize(jsonReader, manifest.SagaEntityType);
        }


        SagaManifest manifest;
        FileStream fileStream;
        bool isCompleted;
        JsonTextWriter jsonWriter;
        JsonTextReader jsonReader;

        const int DefaultBufferSize = 4096;
        static Newtonsoft.Json.JsonSerializer serializer = new Newtonsoft.Json.JsonSerializer();
        static Task<SagaStorageFile> noSagaFoundResult = Task.FromResult<SagaStorageFile>(null);
    }
}