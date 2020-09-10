namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class LearningSynchronizedStorageSession : CompletableSynchronizedStorageSession
    {
        public LearningSynchronizedStorageSession(SagaManifestCollection sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }

        public void Dispose()
        {
            foreach (var sagaFile in sagaFiles.Values)
            {
                sagaFile.Dispose();
            }

            sagaFiles.Clear();
        }

        public async Task CompleteAsync(CancellationToken cancellationToken)
        {
            foreach (var action in deferredActions)
            {
                await action.Execute().ConfigureAwait(false);
            }
            deferredActions.Clear();
        }

        public async Task<TSagaData> Read<TSagaData>(Guid sagaId) where TSagaData : class, IContainSagaData
        {
            var sagaStorageFile = await Open(sagaId, typeof(TSagaData))
                .ConfigureAwait(false);

            if (sagaStorageFile == null)
            {
                return null;
            }

            return await sagaStorageFile.Read<TSagaData>()
                .ConfigureAwait(false);
        }

        public Task Update(IContainSagaData sagaData)
        {
            deferredActions.Add(new UpdateAction(sagaData, sagaFiles, sagaManifests));
            return Task.CompletedTask;
        }

        public Task Save(IContainSagaData sagaData)
        {
            deferredActions.Add(new SaveAction(sagaData, sagaFiles, sagaManifests));
            return Task.CompletedTask;
        }

        public Task Complete(IContainSagaData sagaData)
        {
            deferredActions.Add(new CompleteAction(sagaData, sagaFiles, sagaManifests));
            return Task.CompletedTask;
        }

        async Task<SagaStorageFile> Open(Guid sagaId, Type entityType)
        {
            var sagaManifest = sagaManifests.GetForEntityType(entityType);

            var sagaStorageFile = await SagaStorageFile.Open(sagaId, sagaManifest)
                .ConfigureAwait(false);

            if (sagaStorageFile != null)
            {
                sagaFiles.RegisterSagaFile(sagaStorageFile, sagaId, sagaManifest.SagaEntityType);
            }

            return sagaStorageFile;
        }

        SagaManifestCollection sagaManifests;

        Dictionary<string, SagaStorageFile> sagaFiles = new Dictionary<string, SagaStorageFile>();

        List<StorageAction> deferredActions = new List<StorageAction>();
    }
}