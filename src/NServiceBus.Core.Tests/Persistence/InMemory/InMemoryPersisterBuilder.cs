namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.InMemory.SagaPersister;
    using NServiceBus.Saga;
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
            var inMemorySagaPersister = new InMemorySagaPersister();
            inMemorySagaPersister.Initialize(new SagaMetaModel(TypeBasedSagaMetaModel.Create(sagaTypes, new Conventions())));
            return inMemorySagaPersister;
        }
    }
}