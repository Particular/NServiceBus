namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    class UpdateAction : StorageAction
    {
        public UpdateAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override async Task Execute()
        {
            var sagaFile = GetSagaFile();
            try
            {
                await sagaFile.Write(sagaData)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is ConcurrencyException || ex is IOException)
            {
                throw new Exception($"{nameof(DevelopmentSagaPersister)} concurrency violation: saga entity Id[{sagaData.Id}] already saved.");
            }
        }
    }
}