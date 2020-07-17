namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    [Ignore("Not finished yet")]
    public class When_rolling_back_storage_session : SagaPersisterTests<When_rolling_back_storage_session.TestSaga, When_rolling_back_storage_session.TestSagaData>
    {
        [Test]
        public async Task After_update_should_not_have_stored_anything()
        {
            var correlationPropertyData = Guid.NewGuid().ToString();

            var sagaData = new TestSagaData {SomeId = correlationPropertyData, SomethingWeCareAbout = "NServiceBus"};
            await SaveSaga(sagaData);

            var contextBag = configuration.GetContextBagForSagaStorage();
            using (var session = await configuration.SynchronizedStorage.OpenSession(contextBag))
            {
                var sagaFromStorage = await GetById(sagaData.Id);
                sagaFromStorage.SomethingWeCareAbout = "Particular.Platform";

                await configuration.SagaStorage.Update(sagaFromStorage, session, contextBag);
                await session.CompleteAsync();
                // Do not complete
            }

            var hopefullyNotUpdatedSaga = await GetById(sagaData.Id);

            Assert.NotNull(hopefullyNotUpdatedSaga);
            Assert.That(hopefullyNotUpdatedSaga.SomethingWeCareAbout, Is.EqualTo("NServiceBus"));
        }

        [Test]
        public Task After_save_should_not_have_stored_anything()
        {
            return Task.FromResult(0);
        }

        [Test]
        public Task After_complete_should_not_have_stored_anything()
        {
            return Task.FromResult(0);
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
    }
}