namespace NServiceBus.Features
{
    using System.IO;
    using System.Runtime.Serialization.Json;

    class SagaManifest
    {
        public string StorageDirectory { get; set; }
        public DataContractJsonSerializer Serializer { get; set; }

        public string GetFilePath(string sagaId)
        {
            return Path.Combine(StorageDirectory, sagaId + ".json");
        }
    }
}