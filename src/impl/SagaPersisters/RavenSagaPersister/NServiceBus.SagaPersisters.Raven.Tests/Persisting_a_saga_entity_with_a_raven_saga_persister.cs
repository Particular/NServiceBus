using System;
using NUnit.Framework;
using Raven.Storage.Managed;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using global::Raven.Client.Embedded;

    public abstract class Persisting_a_saga_entity_with_a_raven_saga_persister : Raven_saga_persistence_concern
    {
        protected TestSaga entity;
        protected TestSaga savedEntity;

        [TestFixtureSetUp]
        public override void Setup()
        {
            base.Setup();

            entity = new TestSaga { Id = Guid.NewGuid() };

            SetupEntity(entity);

            WithASagaPersistenceUnitOfWork(p => p.Save(entity));
            
            WithASagaPersistenceUnitOfWork(p => savedEntity = p.Get<TestSaga>(entity.Id));
        }
        
        public abstract void SetupEntity(TestSaga saga);
    }
}