namespace NServiceBus
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Json;
    using System.Threading.Tasks;
    using Extensibility;
    using Persistence;
    using Sagas;

    class DevelopmentSagaPersister : ISagaPersister
    {
        readonly string basePath;

        public DevelopmentSagaPersister(string basePath)
        {
            this.basePath = basePath;
        }
        public Task Save(IContainSagaData sagaData, SagaCorrelationProperty correlationProperty, SynchronizedStorageSession session, ContextBag context)
        {
            var sagaType = sagaData.GetType();
            var serializer = new DataContractJsonSerializer(sagaData.GetType()); //todo: cache

            var dirPath = Path.Combine(basePath, sagaType.FullName.Replace("+", ""),".json");

            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            var filePath = Path.Combine(dirPath, sagaData.Id.ToString());

            using (var sourceStream = new FileStream(filePath,
                FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                //todo: make async
                serializer.WriteObject(sourceStream, sagaData);
            }

            return TaskEx.CompletedTask;
        }

        public Task Update(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            throw new NotImplementedException();
        }

        public Task<TSagaData> Get<TSagaData>(Guid sagaId, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {
            throw new NotImplementedException();
        }

        public Task<TSagaData> Get<TSagaData>(string propertyName, object propertyValue, SynchronizedStorageSession session, ContextBag context) where TSagaData : IContainSagaData
        {

            return Task.FromResult(default(TSagaData));
        }

        public Task Complete(IContainSagaData sagaData, SynchronizedStorageSession session, ContextBag context)
        {
            throw new NotImplementedException();
        }
    }
}