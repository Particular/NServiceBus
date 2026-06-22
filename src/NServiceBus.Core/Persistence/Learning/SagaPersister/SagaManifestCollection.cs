#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Sagas;

class SagaManifestCollection
{
    public SagaManifestCollection(SagaMetadataCollection sagas, string storageLocation, Func<string, string> sagaNameConverter, JsonSerializerOptions? serializerOptions = null)
    {
        serializerOptions ??= new JsonSerializerOptions();
        foreach (var metadata in sagas)
        {
            var sagaStorageDir = Path.Combine(storageLocation, sagaNameConverter(metadata.SagaType.FullName!));

            if (!Directory.Exists(sagaStorageDir))
            {
                Directory.CreateDirectory(sagaStorageDir);
            }

            var manifest = new SagaManifest
            {
                StorageDirectory = sagaStorageDir,
                SagaEntityType = metadata.SagaEntityType,
                SerializerOptions = serializerOptions
            };

            sagaManifests[metadata.SagaEntityType] = manifest;
        }
    }

    public SagaManifest GetForEntityType(Type type) => sagaManifests[type];

    readonly Dictionary<Type, SagaManifest> sagaManifests = [];
}