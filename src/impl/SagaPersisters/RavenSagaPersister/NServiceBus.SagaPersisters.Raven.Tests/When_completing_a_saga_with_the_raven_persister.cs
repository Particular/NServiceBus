namespace NServiceBus.SagaPersisters.Raven.Tests
{
    using System;
    using NUnit.Framework;

    public class When_completing_a_saga_with_the_raven_persister : Raven_saga_persistence_concern
    {

        [Test]
        public void Should_delete_the_saga()
        {
            var saga = new TestSaga { Id = Guid.NewGuid() };

            WithASagaPersistenceUnitOfWork(p => p.Save(saga));
            WithASagaPersistenceUnitOfWork(p => Assert.NotNull(p.Get<TestSaga>(saga.Id)));
            WithASagaPersistenceUnitOfWork(p => p.Complete(saga));
            WithASagaPersistenceUnitOfWork(p => Assert.Null(p.Get<TestSaga>(saga.Id)));
        }
    }
}