namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.InMemory.SagaPersister;
    using NServiceBus.Saga;
    using NServiceBus.Sagas;
    using NUnit.Framework;

    abstract class InMemorySagaPersistenceFixture
    {
        protected InMemorySagaPersister persister;
        protected List<Type> sagaTypes = new List<Type>();

        protected void RegisterSaga<TSaga>() where TSaga : Saga
        {
            sagaTypes.Add(typeof(TSaga));
        }

        [SetUp]
        public void SetUp()
        {
            persister = new InMemorySagaPersister(new SagaMetaModel(TypeBasedSagaMetaModel.Create(sagaTypes,new Conventions())));

        }

    }
}