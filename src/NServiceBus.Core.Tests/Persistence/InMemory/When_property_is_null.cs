namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Saga;
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

            var persister = InMemoryPersisterBuilder.Build<Saga>();
            var options = new SagaPersistenceOptions(SagaMetadata.Create(typeof(Saga)));

            await persister.Save(saga, options);

            var sagaData = await persister.Get<SagaData>("Property", null, options);
            var sagaDataWithPropertyValue = await persister.Get<SagaData>("Property", "a value", options);

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
            public Guid Id { get; set; }

            public string Originator { get; set; }

            public string OriginalMessageId { get; set; }

            public string Property { get; set; }
        }
    }
}