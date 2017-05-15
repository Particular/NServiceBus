namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class SaveAction : StorageAction
    {
        public SaveAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override async Task Execute()
        {
            var sagaId = sagaData.Id;
            var sagaManifest = sagaManifests.GetForEntityType(sagaData.GetType());

            var sagaFile = await SagaStorageFile.Create(sagaId, sagaManifest)
                .ConfigureAwait(false);

            sagaFiles.RegisterSagaFile(sagaFile, sagaId, sagaManifest.SagaEntityType);

            await sagaFile.Write(sagaData)
                .ConfigureAwait(false);
        }
    }
}