namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    abstract class StorageAction
    {
        protected StorageAction(IContainSagaData sagaData, Dictionary<string, SagaStorageFile> sagaFiles, SagaManifestCollection sagaManifests)
        {
            this.sagaFiles = sagaFiles;
            this.sagaData = sagaData;
            this.sagaManifests = sagaManifests;
            sagaFileKey = $"{sagaData.GetType().FullName}{sagaData.Id}";
        }

        public abstract Task Execute(CancellationToken cancellationToken = default);

        protected SagaStorageFile GetSagaFile()
        {
            if (!sagaFiles.TryGetValue(sagaFileKey, out var sagaFile))
            {
                throw new Exception("The saga should be retrieved with the Get method before being updated or completed.");
            }
            return sagaFile;
        }

        protected IContainSagaData sagaData;
        protected Dictionary<string, SagaStorageFile> sagaFiles;
        protected SagaManifestCollection sagaManifests;

        string sagaFileKey;
    }
}