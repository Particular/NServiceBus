using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public partial class When_persisting_a_saga_with_the_same_unique_property_as_a_completed_saga : SagaPersisterTest
    {
        [Test]
        public void It_should_persist_successfully()
        {
            session.Begin();
            var uniqueString = Guid.NewGuid().ToString();
            var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
            persister.Save(saga1);
            session.End();

            session.Begin();
            var saga = persister.Get<SagaData>(saga1.Id);
            persister.Complete(saga);
            session.End();

            session.Begin();
            var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
            persister.Save(saga2);
            session.End();

        }

        public class SagaData : IContainSagaData
        {
            public virtual Guid Id { get; set; }

            public virtual string Originator { get; set; }

            public virtual string OriginalMessageId { get; set; }

            [Unique]
            public virtual string UniqueString { get; set; }
        }
    }
}