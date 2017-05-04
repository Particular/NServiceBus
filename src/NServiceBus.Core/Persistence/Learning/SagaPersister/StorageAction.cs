namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
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

        public abstract Task Execute();

        protected SagaStorageFile GetSagaFile()
        {
            SagaStorageFile sagaFile;
            if (!sagaFiles.TryGetValue(sagaFileKey, out sagaFile))
            {
                throw new Exception("The saga should be retrieved with Get method before it's updated or completed.");
            }
            return sagaFile;
        }

        string sagaFileKey;
        protected IContainSagaData sagaData;
        protected Dictionary<string, SagaStorageFile> sagaFiles;
        protected SagaManifestCollection sagaManifests;
    }
}