﻿namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using Extensibility;
    using NServiceBus.Sagas;
    using NUnit.Framework;
    using Persistence;

    [TestFixture]
    public class When_persisting_different_sagas_with_no_defined_unique_properties : SagaPersisterTests
    {
        [Test]
        public async Task It_should_persist_successfully()
        {
            configuration.RequiresFindersSupport();

            var propertyData = Guid.NewGuid().ToString();
            var saga1 = new SagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            var saga2 = new AnotherSagaWithoutCorrelationPropertyData
            {
                FoundByFinderProperty = propertyData
            };

            var persister = configuration.SagaStorage;
            var savingContextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(savingContextBag))
            {
                await SaveSagaWithSession(saga1, session, savingContextBag);
                await SaveSagaWithSession(saga2, session, savingContextBag);
                await session.CompleteAsync();
            }

            var readContextBag = configuration.GetContextBagForSagaStorage();
            using (var readSession = await configuration.SynchronizedStorage.OpenSession(readContextBag))
            {
                var saga1Result = await persister.Get<SagaWithoutCorrelationPropertyData>(saga1.Id, readSession, readContextBag);

                var saga2Result = await persister.Get<AnotherSagaWithoutCorrelationPropertyData>(saga2.Id, readSession, readContextBag);

                Assert.AreEqual(saga1.FoundByFinderProperty, saga1Result.FoundByFinderProperty);
                Assert.AreEqual(saga2.FoundByFinderProperty, saga2Result.FoundByFinderProperty);
            }
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

        class AnotherSagaWithoutCorrelationProperty : Saga<AnotherSagaWithoutCorrelationPropertyData>,
            IAmStartedByMessages<AnotherSagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task Handle(AnotherSagaWithoutCorrelationPropertyStartingMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<AnotherSagaWithoutCorrelationPropertyData> mapper)
            {
                // no mapping needed
            }
        }

        public class AnotherCustomFinder : IFindSagas<AnotherSagaWithoutCorrelationPropertyData>.Using<AnotherSagaWithoutCorrelationPropertyStartingMessage>
        {
            public Task<AnotherSagaWithoutCorrelationPropertyData> FindBy(AnotherSagaWithoutCorrelationPropertyStartingMessage message, SynchronizedStorageSession storageSession, ReadOnlyContextBag context)
            {
                return Task.FromResult(default(AnotherSagaWithoutCorrelationPropertyData));
            }
        }

        public class AnotherSagaWithoutCorrelationPropertyData : ContainSagaData
        {
            public string FoundByFinderProperty { get; set; }
        }

        public class AnotherSagaWithoutCorrelationPropertyStartingMessage : IMessage
        {
            public string FoundByFinderProperty { get; set; }
        }
    }
}