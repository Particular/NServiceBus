namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using Sagas;

    class SagaManifestCollection
    {
        public SagaManifestCollection(SagaMetadataCollection sagas, string storageLocation)
        {
            foreach (var metadata in sagas)
            {
                var sagaStorageDir = Path.Combine(storageLocation, metadata.SagaType.FullName.Replace("+", ""));

                if (!Directory.Exists(sagaStorageDir))
                {
                    Directory.CreateDirectory(sagaStorageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = sagaStorageDir,
                    Serializer = new DataContractJsonSerializer(metadata.SagaEntityType),
                    SagaEntityType = metadata.SagaEntityType
                };

                sagaManifests[metadata.SagaEntityType] = manifest;
            }
        }

        public SagaManifest GetForEntityType(Type type)
        {
            return sagaManifests[type];
        }

        Dictionary<Type, SagaManifest> sagaManifests = new Dictionary<Type, SagaManifest>();
    }
}