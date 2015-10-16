namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_mapping_to_a_null_property_value
    {
        [Test]
        public async Task Should_retrieve_saga_with_null()
        {
            var sagaId = Guid.NewGuid();
            var saga = new SagaData
            {
                Id = sagaId,
                Property = null
            };

            var persister = new InMemorySagaPersister();

            await persister.Save(saga, SagaMetadataHelper.GetMetadata<Saga>(saga), new ContextBag());

            var sagaData = await persister.Get<SagaData>("Property", null, new ContextBag());
            var sagaDataWithPropertyValue = await persister.Get<SagaData>("Property", "a value", new ContextBag());

            Assert.AreEqual(sagaId, sagaData.Id);
            Assert.IsNull(sagaDataWithPropertyValue);
        }

        class Saga : Saga<SagaData>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
            }
        }

        public class SagaData : IContainSagaData
        {
            public string Property { get; set; }
            public Guid Id { get; set; }

            public string Originator { get; set; }

            public string OriginalMessageId { get; set; }
        }
    }
}