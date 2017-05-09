namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Janitor;
    using Persistence;
    using Sagas;

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

        public async Task CompleteAsync()
        {
            foreach (var action in deferredActions)
            {
                await action.Execute().ConfigureAwait(false);
            }
            deferredActions.Clear();
        }

        public Task<TSagaData> Read<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            SagaStorageFile sagaStorageFile;

            if (!TryOpen(sagaId, typeof(TSagaData), out sagaStorageFile))
            {
                return DefaultSagaDataTask<TSagaData>.Default;
            }

            return Task.FromResult((TSagaData)sagaStorageFile.Read());
        }

        public Task Update(IContainSagaData sagaData)
        {
            deferredActions.Add(new UpdateAction(sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        public Task Save(SagaCorrelationProperty correlationProperty, IContainSagaData sagaData)
        {
            deferredActions.Add(new SaveAction(correlationProperty, sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        public Task Complete(IContainSagaData sagaData)
        {
            deferredActions.Add(new CompleteAction(sagaData, sagaFiles, sagaManifests));
            return TaskEx.CompletedTask;
        }

        bool TryOpen(Guid sagaId, Type entityType, out SagaStorageFile sagaStorageFile)
        {
            var sagaManifest = sagaManifests.GetForEntityType(entityType);

            if (!SagaStorageFile.TryOpen(sagaId, sagaManifest, out sagaStorageFile))
            {
                return false;
            }

            sagaFiles.RegisterSagaFile(sagaStorageFile, sagaId, sagaManifest.SagaEntityType);

            return true;
        }

        SagaManifestCollection sagaManifests;

        Dictionary<string, SagaStorageFile> sagaFiles = new Dictionary<string, SagaStorageFile>();

        List<StorageAction> deferredActions = new List<StorageAction>();

        static class DefaultSagaDataTask<TSagaData>
            where TSagaData : IContainSagaData
        {
            public static Task<TSagaData> Default = Task.FromResult(default(TSagaData));
        }
    }
}