namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NServiceBus.Persistence.Raven;
    using NServiceBus.Persistence.Raven.SagaPersister;
    using NUnit.Framework;
    using Raven.Client;
    using Raven.Client.Embedded;
    using Saga;

    public abstract class Raven_saga_persistence_concern
    {
        protected IDocumentStore store;

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true };
            
            store.Initialize();
        }

        [TestFixtureTearDown]
        public void Teardown()
        {
            store.Dispose();
        }

        public void WithASagaPersistenceUnitOfWork(Action<RavenSagaPersister> action)
        {
            var sessionFactory = new RavenSessionFactory(new StoreAccessor(store));

            try
            {
                var sagaPersister = new RavenSagaPersister(sessionFactory);
                action(sagaPersister);

                sessionFactory.SaveChanges();
            }
            finally 
            {
                sessionFactory.ReleaseSession();
                
            }           
        }

        protected void SaveSaga<T>(T saga) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p => p.Save(saga));
        }

        protected void CompleteSaga<T>(Guid sagaId) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p =>
                                           {
                                               var saga = p.Get<T>(sagaId);
                                               Assert.NotNull(saga, "Could not complete saga. Saga not found");
                                               p.Complete(saga);
                                           });
        }

        protected void UpdateSaga<T>(Guid sagaId, Action<T> update) where T : IContainSagaData
        {
            WithASagaPersistenceUnitOfWork(p =>
            {
                var saga = p.Get<T>(sagaId);
                Assert.NotNull(saga, "Could not update saga. Saga not found");
                update(saga);
                p.Update(saga);
            });
        }
    }
}
