using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_updating_a_saga_property_that_has_a_unique_attribute : SagaPersisterTest
    {
        [Test]
        public void It_should_allow_the_update()
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
            saga.UniqueString = Guid.NewGuid().ToString();
            persister.Update(saga);
            session.End();

            session.Begin();
            var saga2 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

            //this should not blow since we changed the unique value in the previous saga
            persister.Save(saga2);
            session.End();
        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            [Unique]
            public string UniqueString { get; set; }
        }
    }
}