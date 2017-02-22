namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Extensibility;
    using Features;
    using Persistence;
    using Sagas;

    class DevelopmentSagaPersister : ISagaPersister
    {
        readonly Dictionary<Type, SagaManifest> sagaManifests;

        public DevelopmentSagaPersister(Dictionary<Type, SagaManifest> sagaManifests)
        {
            this.sagaManifests = sagaManifests;
        }
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var manifest = sagaManifests[sagaData.GetType()];

            var filePath = manifest.GetFilePath(sagaData.Id.ToString());

            using (var sourceStream = new FileStream(filePath,
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                //todo: make async
                manifest.Serializer.WriteObject(sourceStream, sagaData);
            }

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {

            throw new NotImplementedException();
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var manifest = sagaManifests[typeof(TSagaData)];

            var filePath = manifest.GetFilePath(sagaId.ToString());

            using (var sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //todo: make async
                return Task.FromResult((TSagaData)manifest.Serializer.ReadObject(sourceStream));
            }
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            var manifest = sagaManifests[typeof(TSagaData)];
            var filePath = manifest.GetFilePath(propertyValue.ToString());

            if (!File.Exists(filePath))
            {
                return Task.FromResult(default(TSagaData));
            }

            using (var sourceStream = new FileStream(filePath,
                FileMode.Open, FileAccess.Read, FileShare.None))
            {
                //todo: make async
                return Task.FromResult((TSagaData)manifest.Serializer.ReadObject(sourceStream));
            }
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            var manifest = sagaManifests[sagaData.GetType()];

            var filePath = manifest.GetFilePath(sagaData.Id.ToString());

            File.Delete(filePath);

            return TaskEx.CompletedTask;
        }
    }
}