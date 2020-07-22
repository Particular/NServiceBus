namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixtureSource(typeof(SagaTestVariantSource), "Variants")]
    public class When_persisting_a_saga_with_complex_types : SagaPersisterTests
    {
        [Test]
        public async Task It_should_get_deep_copy()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithComplexTypeEntity {Ints = new List<int> {1, 2}, CorrelationProperty = correlationPropertyData};

            await SaveSaga(sagaData);

            var retrieved = await GetById<SagaWithComplexTypeEntity>(sagaData.Id);

            CollectionAssert.AreEqual(sagaData.Ints, retrieved.Ints);
            Assert.False(ReferenceEquals(sagaData.Ints, retrieved.Ints));
        }

        public class SagaWithComplexType : Saga<SagaWithComplexTypeEntity>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithComplexTypeEntity> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.CorrelationProperty);
            }
        }

        public class SagaWithComplexTypeEntity : ContainSagaData
        {
            public string CorrelationProperty { get; set; }
            public List<int> Ints { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_persisting_a_saga_with_complex_types(TestVariant param) : base(param)
        {
        }
    }
}