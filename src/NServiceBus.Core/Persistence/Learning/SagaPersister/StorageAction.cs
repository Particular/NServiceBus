#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

abstract class StorageAction(
    IContainSagaData sagaData,
    Dictionary<string, SagaStorageFile> sagaFiles,
    SagaManifestCollection sagaManifests)
{
    public abstract Task Execute(CancellationToken cancellationToken = default);

    protected SagaStorageFile GetSagaFile() => !sagaFiles.TryGetValue(sagaFileKey, out var sagaFile)
        ? throw new Exception("The saga should be retrieved with the Get method before being updated or completed.")
        : sagaFile;

    protected readonly IContainSagaData sagaData = sagaData;
    protected readonly Dictionary<string, SagaStorageFile> sagaFiles = sagaFiles;
    protected readonly SagaManifestCollection sagaManifests = sagaManifests;

    readonly string sagaFileKey = $"{sagaData.GetType().FullName}{sagaData.Id}";
}