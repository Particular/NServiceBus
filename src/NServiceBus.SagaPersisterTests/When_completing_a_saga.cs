using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public partial class When_completing_a_saga : SagaPersisterTest
    {

        [Test]
        public void Should_delete_the_saga()
        {
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