namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Sagas;

    class SagaManifestCollection
    {
        public SagaManifestCollection(SagaMetadataCollection sagas, string storageLocation, Func<string, string> sagaNameConverter)
        {
            foreach (var metadata in sagas)
            {
                var sagaStorageDir = Path.Combine(storageLocation, sagaNameConverter(metadata.SagaType.FullName));

                if (!Directory.Exists(sagaStorageDir))
                {
                    Directory.CreateDirectory(sagaStorageDir);
                }

                var manifest = new SagaManifest
                {
                    StorageDirectory = sagaStorageDir,
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