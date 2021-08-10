namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;

    [SkipWeaving]
    class LearningSynchronizedStorageSession : ICompletableSynchronizedStorageSession
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

        public async Task CompleteAsync(CancellationToken cancellationToken = default)
        {
            foreach (var action in deferredActions)
            {
                await action.Execute(cancellationToken).ConfigureAwait(false);
            }
            deferredActions.Clear();
        }

        public async Task<TSagaData> Read<TSagaData>(Guid sagaId, CancellationToken cancellationToken = default) where TSagaData : class, IContainSagaData
        {
            var sagaStorageFile = await Open(sagaId, typeof(TSagaData), cancellationToken)
                .ConfigureAwait(false);

            if (sagaStorageFile == null)
            {
                return null;
            }

            return await sagaStorageFile.Read<TSagaData>(cancellationToken)
                .ConfigureAwait(false);
        }

        public void Update(IContainSagaData sagaData)
        {
            deferredActions.Add(new UpdateAction(sagaData, sagaFiles, sagaManifests));
        }

        public void Save(IContainSagaData sagaData)
        {
            deferredActions.Add(new SaveAction(sagaData, sagaFiles, sagaManifests));
        }

        public void Complete(IContainSagaData sagaData)
        {
            deferredActions.Add(new CompleteAction(sagaData, sagaFiles, sagaManifests));
        }

        async Task<SagaStorageFile> Open(Guid sagaId, Type entityType, CancellationToken cancellationToken)
        {
            var sagaManifest = sagaManifests.GetForEntityType(entityType);

            var sagaStorageFile = await SagaStorageFile.Open(sagaId, sagaManifest, cancellationToken)
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