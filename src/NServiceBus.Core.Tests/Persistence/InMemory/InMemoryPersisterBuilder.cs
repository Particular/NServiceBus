namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    class InMemoryPersisterBuilder
    {
        [SetUp]
        public static InMemorySagaPersister Build<TSaga>() where TSaga : Saga
        {
            return Build(typeof(TSaga));
        }

        public static InMemorySagaPersister Build(params Type[] sagaTypes)
        {
            var sagaMetaModel = new SagaMetadataCollection();
            sagaMetaModel.Initialize(sagaTypes, new Conventions());

            var inMemorySagaPersister = new InMemorySagaPersister(sagaMetaModel);
            return inMemorySagaPersister;
        }
    }
}