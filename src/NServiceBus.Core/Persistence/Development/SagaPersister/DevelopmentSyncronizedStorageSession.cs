namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class DevelopmentSyncronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public void Dispose()
        {
            foreach (var sagaFile in sagaFiles.Values)
            {
                sagaFile.Dispose();
            }
        }

        public Task CompleteAsync()
        {
            foreach (var sagaFile in sagaFiles.Values)
            {
                sagaFile.Complete();
            }

            return TaskEx.CompletedTask;
        }

        public bool TryOpenAndLockSaga(Guid sagaId, SagaManifest sagaManifest, out SagaStorageFile sagaStorageFile)
        {
            if (!SagaStorageFile.TryOpen(sagaId, sagaManifest, out sagaStorageFile))
            {
                return false;
            }

            RegisterSagaFile(sagaStorageFile, sagaId, sagaManifest.SagaEntityType);

            return true;
        }


        public SagaStorageFile CreateNew(Guid sagaId, SagaManifest sagaManifest)
        {
            var sagaFile = SagaStorageFile.Create(sagaId, sagaManifest);

            RegisterSagaFile(sagaFile, sagaId, sagaManifest.SagaEntityType);

            return sagaFile;
        }

        public SagaStorageFile GetSagaFile(IContainSagaData sagaData)
        {
            return sagaFiles[sagaData.GetType().FullName + sagaData.Id];
        }

        void RegisterSagaFile(SagaStorageFile sagaStorageFile, Guid sagaId, Type sagaDataType)
        {
            sagaFiles[sagaDataType.FullName + sagaId] = sagaStorageFile;
        }

        Dictionary<string, SagaStorageFile> sagaFiles = new Dictionary<string, SagaStorageFile>();
    }
}