namespace NServiceBus
{
    using System;
    using System.IO;
    using Janitor;

    [SkipWeaving]
    class SagaStorageFile
    {
        SagaStorageFile(FileStream fileStream, SagaManifest manifest)
        {
            this.fileStream = fileStream;
            this.manifest = manifest;
        }

        public static bool TryOpen(Guid sagaId, SagaManifest manifest, out SagaStorageFile sagaStorageFile)
        {
            var filePath = manifest.GetFilePath(sagaId);

            if (!File.Exists(filePath))
            {
                sagaStorageFile = null;
                return false;
            }

            sagaStorageFile = new SagaStorageFile(new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None), manifest);

            return true;
        }

        public static SagaStorageFile Create(Guid sagaId, SagaManifest manifest)
        {
            var filePath = manifest.GetFilePath(sagaId);

            return new SagaStorageFile(new FileStream(filePath, FileMode.CreateNew, FileAccess.Write, FileShare.None), manifest);
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

        public void Complete()
        {
            fileStream.Position = 0;
            manifest.Serializer.WriteObject(fileStream, sagaToWrite);
        }

        public void Delete()
        {
            isCompleted = true;
        }

        public object Read()
        {
            return manifest.Serializer.ReadObject(fileStream);
        }

        public void Write(IContainSagaData sagaData)
        {
            Console.Out.WriteLine($"{fileStream.Name} - write");

            sagaToWrite = sagaData;
        }

        readonly SagaManifest manifest;
        FileStream fileStream;
        bool isCompleted;
        object sagaToWrite;
    }
}