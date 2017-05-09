namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class CompleteAction : StorageAction
    {
        public CompleteAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override async Task Execute()
        {
            var sagaFile = GetSagaFile();

            try
            {
                await sagaFile.MarkAsCompleted()
                    .ConfigureAwait(false);
            }
            catch (LearningSagaPersisterConcurrencyException)
            {
                throw new Exception("Saga can't be completed because it was updated by another process.");
            }
        }
    }
}