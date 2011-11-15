using System;
using NUnit.Framework;
using Raven.Storage.Managed;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using global::Raven.Client.Embedded;

    public abstract class Persisting_a_saga_entity_with_a_raven_saga_persister
    {
        protected TestSaga entity;
        protected TestSaga savedEntity;
        TransactionalStorage storage;

        [TestFixtureSetUp]
        public void Setup()
        {
            var store = new EmbeddableDocumentStore { RunInMemory = true, DataDirectory = Guid.NewGuid().ToString() };
            store.Initialize();

            entity = new TestSaga();
            entity.Id = Guid.NewGuid();

            SetupEntity(entity);

            var persister = new RavenSagaPersister { Store = store };

            persister.Save(entity);

            savedEntity = persister.Get<TestSaga>(entity.Id);
        }

        public abstract void SetupEntity(TestSaga saga);
    }
}