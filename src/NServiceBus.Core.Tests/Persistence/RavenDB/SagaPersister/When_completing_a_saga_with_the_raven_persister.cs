namespace NServiceBus.Core.Tests.Persistence.RavenDB.SagaPersister
{
    using System;
    using NUnit.Framework;

    public class When_completing_a_saga_with_the_raven_persister : Raven_saga_persistence_concern
    {

        [Test]
        public void Should_delete_the_saga()
        {
            var sagaId = Guid.NewGuid();

            SaveSaga(new TestSaga { Id = sagaId });
            CompleteSaga<TestSaga>(sagaId);

            WithASagaPersistenceUnitOfWork(p => Assert.Null(p.Get<TestSaga>(sagaId)));
        }
    }
}