namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    class CompleteAction : StorageAction
    {
        public CompleteAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override Task Execute()
        {
            var sagaFile = GetSagaFile();

            return sagaFile.MarkAsCompleted();
        }
    }
}