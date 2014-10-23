using System;
using NServiceBus.Saga;
using NUnit.Framework;

namespace NServiceBus.SagaPersisterTests
{
    [TestFixture]
// ReSharper disable once PartialTypeWithSinglePart
    public partial class When_updating_a_saga_property_that_does_not_have_a_unique_attribute : SagaPersisterTest
    {
        [Test]
        public void It_should_persist_successfully()
        {
            session.Begin();
            var uniqueString = Guid.NewGuid().ToString();

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
            persister.Update(saga);
            session.End();
        }


        public class SagaData : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            [Unique]
            public string UniqueString { get; set; }

            public string NonUniqueString { get; set; }
        }
    }
}