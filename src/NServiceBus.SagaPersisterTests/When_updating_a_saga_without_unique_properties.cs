using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_updating_a_saga_without_unique_properties
    {
        [Test]
        public void It_should_persist_successfully()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            session.Begin();
            var uniqueString = Guid.NewGuid().ToString();
            var anotherUniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString,
                NonUniqueString = "notUnique"
            };
            persister.Save(saga1);
            session.End();

            session.Begin();
            var saga = persister.Get<SagaData>(saga1.Id);
            saga.NonUniqueString = "notUnique2";
            saga.UniqueString = anotherUniqueString;
            persister.Update(saga);
            session.End();
        }

        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            public string UniqueString { get; set; }
            public string NonUniqueString { get; set; }
        }
    }
}