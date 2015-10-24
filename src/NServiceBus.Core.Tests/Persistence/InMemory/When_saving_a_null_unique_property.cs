namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using System.Threading.Tasks;
    using NServiceBus.Extensibility;
    using NUnit.Framework;

    [TestFixture]
    class When_saving_a_null_unique_property
    {
        [Test]
        public void Should_throw()
        {
            var sagaId = new Guid("895e60e0-7be3-490a-afca-fe69184474ca");
            var saga = new SagaData
            {
                Id = sagaId,
                Property = null
            };

            var persister = new InMemorySagaPersister();

            var exception = Assert.Throws<InvalidOperationException>(() => persister.Save(saga, SagaMetadataHelper.GetMetadata<Saga>(saga), new ContextBag()));
            Assert.AreEqual("Cannot store saga with id '895e60e0-7be3-490a-afca-fe69184474ca' since the unique property 'Property' has a null value.", exception.Message);
        }

        class Saga : Saga<SagaData>, IAmStartedByMessages<M1>
        {
            public Task Handle(M1 message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaData> mapper)
            {
                mapper.ConfigureMapping<M1>(m => m.Property).ToSaga(s => s.Property);
            }
        }

        class M1
        {
            public string Property { get; set; }
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