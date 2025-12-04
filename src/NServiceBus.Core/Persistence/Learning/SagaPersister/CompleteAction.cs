#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

class CompleteAction(
    IContainSagaData sagaData,
    Dictionary<string, SagaStorageFile> sagaFiles,
    SagaManifestCollection sagaManifests)
    : StorageAction(sagaData, sagaFiles, sagaManifests)
{
    public override Task Execute(CancellationToken cancellationToken = default)
    {
        var sagaFile = GetSagaFile();

        sagaFile.MarkAsCompleted();

        return Task.CompletedTask;
    }
}