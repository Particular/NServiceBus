namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_updating_a_saga_with_no_mapping_found_by_id : SagaPersisterTests<When_updating_a_saga_with_no_mapping_found_by_id.SagaWithoutCorrelationProperty, When_updating_a_saga_with_no_mapping_found_by_id.SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            configuration.RequiresFindersSupport();

            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                SomeSagaProperty = Guid.NewGuid().ToString(),
            };

            await SaveSaga(sagaData);

            var updateValue = Guid.NewGuid().ToString();
            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                sagaData = await configuration.SagaStorage.Get<SagaWithoutCorrelationPropertyData>(sagaData.Id, completeSession, context);
                sagaData.SomeSagaProperty = updateValue;

                await configuration.SagaStorage.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var result = await GetById(sagaData.Id);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.SomeSagaProperty, Is.EqualTo(updateValue));
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
            }

            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }
        }

        public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
            {
                throw new NotImplementedException();
            }
        }

        public class SagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string SomeSagaProperty { get; set; }
        }

        public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
        }
    }
}