using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_storing_a_saga_with_a_long_namespace
    {
        [Test]
        public void Should_not_generate_a_to_long_unique_property_id()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            session.Begin();
            var uniqueString = Guid.NewGuid().ToString();
            var saga = new SagaWithUniquePropertyAndALongNamespace
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };
            persister.Save(saga);
            session.End();
        }

        public class SagaWithUniquePropertyAndALongNamespace : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            [Unique]
            public string UniqueString { get; set; }

        }
    }
}