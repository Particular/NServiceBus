using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
    public class When_persisting_a_saga_with_unique_property_set_to_null : SagaPersisterTest
    {
        [Test, ExpectedException(typeof(ArgumentNullException))]
        public void should_throw_a_ArgumentNullException()
        {
            var saga1 = new SagaWithUniqueProperty
            {
                Id = Guid.NewGuid(),
                UniqueString = null
            };

            session.Begin();
            persister.Save(saga1);
            session.End();
        }
        public class SagaWithUniqueProperty : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }
            [Unique]
            public string UniqueString { get; set; }

        }
    }
}