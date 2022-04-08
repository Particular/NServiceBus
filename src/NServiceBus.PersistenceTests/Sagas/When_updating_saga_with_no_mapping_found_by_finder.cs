namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    public class When_updating_saga_with_no_mapping_found_by_finder : SagaPersisterTests
    {
        [Test]
        public async Task It_should_successfully_update_the_saga()
        {
            configuration.RequiresFindersSupport();

            var saga = new SagaWithoutCorrelationPropertyData()
            {
                SomeSagaProperty = "initial value"
            };
            await SaveSaga(saga);

            var updateValue = Guid.NewGuid().ToString();
            var context = configuration.GetContextBagForSagaStorage();
            using (var completeSession = configuration.CreateStorageSession())
            {
                await completeSession.OpenSession(context);

                // the saga won't be loaded via a persister.Get operation in this case
                var customFinder = new CustomFinder(saga);
                var sagaData = await customFinder.FindBy(new SagaWithoutCorrelationPropertyStartingMessage(), completeSession, context);
                sagaData.SomeSagaProperty = updateValue;

                await configuration.SagaStorage.Update(sagaData, completeSession, context);
                await completeSession.CompleteAsync();
            }

            var result = await GetById<SagaWithoutCorrelationPropertyData>(saga.Id);
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

        public class CustomFinder : ISagaFinder<SagaWithoutCorrelationPropertyData, SagaWithoutCorrelationPropertyStartingMessage>
        {
            readonly SagaWithoutCorrelationPropertyData sagaToFind;

            public CustomFinder(SagaWithoutCorrelationPropertyData sagaToFind)
            {
                this.sagaToFind = sagaToFind;
            }

            public Task<SagaWithoutCorrelationPropertyData> FindBy(SagaWithoutCorrelationPropertyStartingMessage message, ISynchronizedStorageSession storageSession, IReadOnlyContextBag context, CancellationToken cancellationToken = default)
            {
                return Task.FromResult(sagaToFind);
            }
        }

        public class SagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string SomeSagaProperty { get; set; }
        }

        public class SagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
        }

        public When_updating_saga_with_no_mapping_found_by_finder(TestVariant param) : base(param)
        {
        }
    }
}