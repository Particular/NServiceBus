using System;
using NUnit.Framework;
using Raven.Client.Embedded;

namespace NServiceBus.SagaPersisters.Raven.Tests
{
    public abstract class Raven_saga_persistence_concern
    {
        protected RavenSagaPersister SagaPersister;

        [TestFixtureSetUp]
        public virtual void Setup()
        {
            var store = new EmbeddableDocumentStore { RunInMemory = true, DataDirectory = Guid.NewGuid().ToString() };
            store.Initialize();
            
            SagaPersister = new RavenSagaPersister { Store = store };
        }

        
    }
}