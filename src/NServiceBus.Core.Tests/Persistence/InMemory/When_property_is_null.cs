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
        public void Should_throw()
        {
            var sagaId = Guid.NewGuid();
            var saga = new SagaData
            {
                Id = sagaId,
                Property = null
            };

            var persister = new InMemorySagaPersister();

            Assert.Throws<InvalidOperationException>(async () => await persister.Save(saga, SagaMetadataHelper.GetMetadata<Saga>(saga), new ContextBag()));
        }

        class Saga : Saga<SagaData>,IAmStartedByMessages<StartMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<MyMessage>(m => m.Property)
                    .ToSaga(s => s.Property);
            }

            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class SagaData : IContainSagaData
        {
            public string Property { get; set; }
            public Guid Id { get; set; }

            public string Originator { get; set; }

            public string OriginalMessageId { get; set; }
        }

        class MyMessage
        {
            public string Property { get; set; }
        }
    }
}