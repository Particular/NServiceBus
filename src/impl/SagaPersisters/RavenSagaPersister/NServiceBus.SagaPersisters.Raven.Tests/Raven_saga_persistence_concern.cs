using System;
using NServiceBus.Persistence.Raven;
using NUnit.Framework;
using Raven.Client;
using Raven.Client.Embedded;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public abstract class Raven_saga_persistence_concern
    {
        IDocumentStore store;

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            store = new EmbeddableDocumentStore { RunInMemory = true, DataDirectory = Guid.NewGuid().ToString() };
            store.Initialize();
        }

        public void WithASagaPersistenceUnitOfWork(Action<RavenSagaPersister> action)
        {
            using (var sessionFactory = new RavenSessionFactory(store))
            {
                var sagaPersister = new RavenSagaPersister(sessionFactory);
                action(sagaPersister);
                sessionFactory.Session.SaveChanges();
            }
        }
    }
}