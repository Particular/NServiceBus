#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Sagas;

class SaveAction(
    IContainSagaData sagaData,
    IReadOnlyList<StoredSagaTimeout> timeouts,
    Dictionary<string, SagaStorageFile> sagaFiles,
    SagaManifestCollection sagaManifests)
    : StorageAction(sagaData, sagaFiles, sagaManifests)
{
    public override async Task Execute(CancellationToken cancellationToken = default)
    {
        var sagaId = sagaData.Id;
        var sagaManifest = sagaManifests.GetForEntityType(sagaData.GetType());

        var sagaFile = await SagaStorageFile.Create(sagaId, sagaManifest, cancellationToken)
            .ConfigureAwait(false);

        sagaFiles.RegisterSagaFile(sagaFile, sagaId, sagaManifest.SagaEntityType);

        await sagaFile.Write(sagaData, timeouts, cancellationToken)
            .ConfigureAwait(false);
    }
}