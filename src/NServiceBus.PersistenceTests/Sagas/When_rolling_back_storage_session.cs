namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_rolling_back_storage_session : SagaPersisterTests
    {
        [Test]
        public async Task Should_rollback_updates()
        {
            var sagaData = new TestSagaData
            {
                SomeId = Guid.NewGuid().ToString(),
                SomethingWeCareAbout = "NServiceBus"
            };
            await SaveSaga(sagaData);

            var contextBag = configuration.GetContextBagForSagaStorage();
            using (var session = configuration.CreateStorageSession())
            {
                await session.OpenSession(contextBag);

                var sagaFromStorage = await configuration.SagaStorage.Get<TestSagaData>(sagaData.Id, session, contextBag);
                sagaFromStorage.SomethingWeCareAbout = "Particular.Platform";

                await configuration.SagaStorage.Update(sagaFromStorage, session, contextBag);

                // Do not complete
            }

            var hopefullyNotUpdatedSaga = await GetById<TestSagaData>(sagaData.Id);

            Assert.NotNull(hopefullyNotUpdatedSaga);
            Assert.That(hopefullyNotUpdatedSaga.SomethingWeCareAbout, Is.EqualTo("NServiceBus"));
        }

        [Test]
        public async Task Should_rollback_storing_new_saga()
        {
            var sagaData = new TestSagaData
            {
                SomeId = Guid.NewGuid().ToString(),
                SomethingWeCareAbout = "NServiceBus"
            };

            var contextBag = configuration.GetContextBagForSagaStorage();
            using (var session = configuration.CreateStorageSession())
            {
                await session.OpenSession(contextBag);

                await SaveSagaWithSession(sagaData, session, contextBag);
                // Do not complete
            }

            var sagaById = await GetById<TestSagaData>(sagaData.Id);
            Assert.IsNull(sagaById);
            var sagaByCorrelationProperty = await GetByCorrelationProperty<TestSagaData>(nameof(sagaData.SomeId), sagaData.SomeId);
            Assert.IsNull(sagaByCorrelationProperty);
        }

        [Test]
        public async Task Should_rollback_saga_completion()
        {
            var sagaData = new TestSagaData
            {
                SomeId = Guid.NewGuid().ToString(),
                SomethingWeCareAbout = "NServiceBus"
            };
            await SaveSaga(sagaData);

            var contextBag = configuration.GetContextBagForSagaStorage();
            using (var session = configuration.CreateStorageSession())
            {
                await session.OpenSession(contextBag);

                var sagaFromStorage = await configuration.SagaStorage.Get<TestSagaData>(sagaData.Id, session, contextBag);

                await configuration.SagaStorage.Complete(sagaFromStorage, session, contextBag);

                // Do not complete
            }

            var nonCompletedSaga = await GetById<TestSagaData>(sagaData.Id);

            Assert.NotNull(nonCompletedSaga);
            Assert.That(nonCompletedSaga.SomethingWeCareAbout, Is.EqualTo("NServiceBus"));
        }

        public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
        {
            public Task Handle(StartMessage message, IMessageHandlerContext context)
            {
                throw new NotImplementedException();
            }

            protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper)
            {
                mapper.ConfigureMapping<StartMessage>(msg => msg.SomeId).ToSaga(saga => saga.SomeId);
            }
        }

        public class TestSagaData : ContainSagaData
        {
            public string SomeId { get; set; } = "Test";

            public string SomethingWeCareAbout { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_rolling_back_storage_session(TestVariant param) : base(param)
        {
        }
    }
}