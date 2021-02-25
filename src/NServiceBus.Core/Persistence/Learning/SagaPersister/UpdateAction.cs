namespace NServiceBus
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    class UpdateAction : StorageAction
    {
        public UpdateAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests) : base(sagaData, sagaFiles, sagaManifests)
        {
        }

        public override Task Execute(CancellationToken cancellationToken = default)
        {
            var sagaFile = GetSagaFile();

            return sagaFile.Write(sagaData, cancellationToken);
        }
    }
}