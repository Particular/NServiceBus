using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_completing_a_saga_with_the_raven_persister
    {

        [Test]
        public void Should_delete_the_saga()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            var sagaId = Guid.NewGuid();

            session.Begin();
            persister.Save(new SagaData
            {
                Id = sagaId
            });
            session.End();

            session.Begin();
            var saga = persister.Get<SagaData>(sagaId);
            persister.Complete(saga);
            session.End();

            Assert.Null(persister.Get<SagaData>(sagaId));
        
        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
        }
    }
}