using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{

    [TestFixture]
    public class When_persisting_a_saga_with_the_same_unique_property_as_another_saga
    {
        [Test]
        public void It_should_enforce_uniqueness()
        {
            var persisterAndSession = TestSagaPersister.ConstructPersister();
            var persister = persisterAndSession.Item1;
            var session = persisterAndSession.Item2;

            session.Begin();
            var uniqueString = Guid.NewGuid().ToString();

            var saga1 = new SagaData
            {
                Id = Guid.NewGuid(),
                UniqueString = uniqueString
            };

            persister.Save(saga1);
            session.End();


            try
            {
                session.Begin();
                var saga2 = new SagaData
                {
                    Id = Guid.NewGuid(),
                    UniqueString = uniqueString
                };
                persister.Save(saga2);
                session.End();
                Assert.Fail("Expected exception");
            }
// ReSharper disable once EmptyGeneralCatchClause
            catch (Exception)
            {
                //All good
            }
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