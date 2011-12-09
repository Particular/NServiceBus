namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using System;
    using NUnit.Framework;
    using global::Raven.Client.Embedded;

    public class When_completing_a_saga_with_the_raven_persister
    {
      
        [Test]
        public void Should_delete_the_saga()
        {
            var store = new EmbeddableDocumentStore { RunInMemory = true, DataDirectory = Guid.NewGuid().ToString() };
            store.Initialize();

            var saga = new TestSaga {Id = Guid.NewGuid()};

            var persister = new RavenSagaPersister { Store = store };

            persister.Save(saga);

            Assert.NotNull(persister.Get<TestSaga>(saga.Id)); 

            persister.Complete(saga);

            Assert.Null(persister.Get<TestSaga>(saga.Id)); 

        }
    }
}