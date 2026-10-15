#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sagas;

class UpdateAction(
    IContainSagaData sagaData,
    IReadOnlyList<StoredSagaTimeout> timeouts,
    Dictionary<string, SagaStorageFile> sagaFiles,
    SagaManifestCollection sagaManifests)
    : StorageAction(sagaData, sagaFiles, sagaManifests)
{
    public override Task Execute(CancellationToken cancellationToken = default)
    {
        var sagaFile = GetSagaFile();

        return sagaFile.Write(sagaData, timeouts, cancellationToken);
    }
}