namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_updating_a_saga_with_no_defined_unique_property : SagaPersisterTests<When_updating_a_saga_with_no_defined_unique_property.SagaWithoutCorrelationProperty, When_updating_a_saga_with_no_defined_unique_property.SagaWithoutCorrelationPropertyData>
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var sagaData = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData,
                DateTimeProperty = DateTime.UtcNow
            };

            var finder = typeof(CustomFinder);
            await SaveSaga(sagaData, finder);

            var updateValue = Guid.NewGuid().ToString();
            var context = configuration.GetContextBagForSagaStorage();
            var persister = configuration.SagaStorage;
            using (var completeSession = await configuration.SynchronizedStorage.OpenSession(context))
            {
                sagaData = await persister.Get<SagaWithoutCorrelationPropertyData>(sagaData.Id, completeSession, context);

                sagaData.FoundByFinderProperty = updateValue;

                await persister.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var result = await GetById(sagaData.Id, finder);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.FoundByFinderProperty, Is.EqualTo(updateValue));
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
            }
        }

        public class CustomFinder : IFindSagas<SagaWithoutCorrelationPropertyData>.Using<SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
            {
                return Task.FromResult(default(SagaWithoutCorrelationPropertyData));
            }
        }

        public class SagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string FoundByFinderProperty { get; set; }

            public DateTime DateTimeProperty { get; set; }
        }

        public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
            public string FoundByFinderProperty { get; set; }
        }
    }
}