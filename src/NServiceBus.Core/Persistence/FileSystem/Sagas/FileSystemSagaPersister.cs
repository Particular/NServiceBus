using System;

namespace NServiceBus.Persistence.FileSystem.Sagas
{
    using System.IO;
    using NServiceBus.Saga;

    class FileSystemSagaPersister : ISagaPersister
    {
        // TODO Saga serialization / deserialization

        string sagasStoragePath = @"z:\sagas\";

        public void Save(IContainSagaData saga)
        {
            // TODO file locking

            File.WriteAllText(Path.Combine(sagasStoragePath, saga.Id.ToString()), saga.ToString()); // TODO
        }

        public void Update(IContainSagaData saga)
        {
            Save(saga);
        }

        public TSagaData Get<TSagaData>(Guid sagaId) where TSagaData : IContainSagaData
        {
            throw new NotImplementedException();
        }

        public TSagaData Get<TSagaData>(string propertyName, object propertyValue) where TSagaData : IContainSagaData
        {
            // TODO do we really want to iterate through all files, deserialize and compare values?

            throw new NotImplementedException();
        }

        public void Complete(IContainSagaData saga)
        {
            throw new NotImplementedException();
        }
    }
}
