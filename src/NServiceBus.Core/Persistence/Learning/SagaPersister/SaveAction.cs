namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class SaveAction : StorageAction
    {
        public SaveAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override async Task Execute(CancellationToken cancellationToken)
        {
            var sagaId = sagaData.Id;
            var sagaManifest = sagaManifests.GetForEntityType(sagaData.GetType());

            var sagaFile = await SagaStorageFile.Create(sagaId, sagaManifest, cancellationToken)
                .ConfigureAwait(false);

            sagaFiles.RegisterSagaFile(sagaFile, sagaId, sagaManifest.SagaEntityType);

            await sagaFile.Write(sagaData, cancellationToken)
                .ConfigureAwait(false);
        }
    }
}