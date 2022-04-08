namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    public class When_completing_saga_with_no_mapping_loaded_by_id : SagaPersisterTests
    {
        [Test]
        public async Task It_should_successfully_remove_the_saga()
        {
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var saga = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            await SaveSaga(saga);

            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.OpenSession(context);

                var sagaData = await configuration.SagaStorage.Get<SagaWithoutCorrelationPropertyData>(saga.Id, completeSession, context);

                await configuration.SagaStorage.Complete(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var result = await GetById<SagaWithoutCorrelationPropertyData>(saga.Id);
            Assert.That(result, Is.Null);
        }

        public class SagaWithoutCorrelationProperty : Saga<SagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<SagaWithoutCorrelationPropertyStartingMessage>
        {
            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<SagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping provided
            }

            public Task Handle(SagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
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

        public class CustomFinder : ISagaFinder<SagaWithoutCorrelationPropertyData, SagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                throw new NotImplementedException();
            }
        }

        public When_completing_saga_with_no_mapping_loaded_by_id(TestVariant param) : base(param)
        {
        }
    }
}