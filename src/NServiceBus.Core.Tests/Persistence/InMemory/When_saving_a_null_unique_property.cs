namespace NServiceBus.SagaPersisters.InMemory.Tests
{
    using System;
    using NServiceBus.Saga;
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

            var persister = InMemoryPersisterBuilder.Build<Saga>();
            var exception = Assert.Throws<InvalidOperationException>(() => persister.Save(saga));
            Assert.AreEqual("Cannot store saga with id '895e60e0-7be3-490a-afca-fe69184474ca' since the unique property 'Property' has a null value.", exception.Message);
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

            [Unique]
            public string Property { get; set; }
        }
    }
}