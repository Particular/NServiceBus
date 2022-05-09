namespace NServiceBus.PersistenceTesting.Sagas
{
#if NET
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_persisting_a_saga_with_record_type : SagaPersisterTests
    {
        [Test]
        public async Task It_should_get_deep_copy()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithRecordTypeEntity
            {
                Ints = new List<int> { 1, 2 },
                CorrelationProperty = correlationPropertyData
            };

            await SaveSaga(sagaData);

            var retrieved = await GetById<SagaWithRecordTypeEntity>(sagaData.Id);

            CollectionAssert.AreEqual(sagaData.Ints, retrieved.Ints);
            Assert.False(ReferenceEquals(sagaData.Ints, retrieved.Ints));
        }

        [Test]
        public async Task It_Should_Load()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var person = new SagaWithNestedRecordTypeEntity.Person() { FirstName = "John", LastName = "Doe" };
            var sagaData =
                new SagaWithNestedRecordTypeEntity { SomePerson = person, CorrelationProperty = correlationPropertyData };

            await SaveSaga(sagaData);

            var retrieved = await GetById<SagaWithNestedRecordTypeEntity>(sagaData.Id);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(person, retrieved.SomePerson);
        }

        public class SagaWithRecordType : Saga<SagaWithRecordTypeEntity>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithRecordTypeEntity> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.CorrelationProperty);
            }
        }

        public class SagaWithNestedRecordType : Saga<SagaWithNestedRecordTypeEntity>,
            IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithNestedRecordTypeEntity> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.CorrelationProperty);
            }
        }

        public class SagaWithNestedRecordTypeEntity : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public Person SomePerson { get; set; }

            // A mutable record type. Immutable won't deserialize
            public record Person
            {
                public string FirstName { get; set; }
                public string LastName { get; set; }
            };
        }

        public record SagaWithRecordTypeEntity : IContainSagaData
        {
            public Guid Id { get; set; }
            public string Originator { get; set; }
            public string OriginalMessageId { get; set; }

            public string CorrelationProperty { get; set; }
            public List<int> Ints { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_persisting_a_saga_with_record_type(TestVariant param) : base(param)
        {

        }
    }
#endif
}