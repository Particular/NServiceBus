namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_concurrent_update_exceed_lock_request_timeout_pessimistic : SagaPersisterTests
    {
        public override async Task OneTimeSetUp()
        {
            configuration = new PersistenceTestsConfiguration(param, TimeSpan.FromMilliseconds(100));
            await configuration.Configure();
        }

        [Test]
        public async Task Should_fail_with_timeout()
        {
            configuration.RequiresPessimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData {SomeId = correlationPropertyData, SagaProperty = "initial value"};
            await SaveSaga(saga);

            var firstSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var persister = configuration.SagaStorage;

            async Task FirstSession()
            {
                var firstSessionContext = configuration.GetContextBagForSagaStorage();
                using (var firstSaveSession = await configuration.SynchronizedStorage.OpenSession(firstSessionContext))
                {
                    var record = await persister.Get<TestSagaData>(saga.Id, firstSaveSession, firstSessionContext);
                    firstSessionGetDone.SetResult(true);

                    await Task.Delay(200).ConfigureAwait(false);

                    record.SagaProperty = "session 1 value";
                    await persister.Update(record, firstSaveSession, firstSessionContext);
                    await secondSessionGetDone.Task.ConfigureAwait(false);
                    await firstSaveSession.CompleteAsync();
                }
            }

            async Task SecondSession()
            {
                var secondContext = configuration.GetContextBagForSagaStorage();
                var secondSession = await configuration.SynchronizedStorage.OpenSession(secondContext);
                try
                {
                    await firstSessionGetDone.Task.ConfigureAwait(false);

                    var recordTask = persister.Get<TestSagaData>(saga.Id, secondSession, secondContext);
                    secondSessionGetDone.SetResult(true);
                    var record = await recordTask.ConfigureAwait(false);
                    record.SagaProperty = "session 2 value";
                    await persister.Update(record, secondSession, secondContext);
                    await secondSession.CompleteAsync();
                }
                finally
                {
                    secondSession.Dispose();
                }
            }

            var firstSessionTask = FirstSession();
            var secondSessionTask = SecondSession();

            Assert.DoesNotThrowAsync(async () => await firstSessionTask);
            Assert.ThrowsAsync<TimeoutException>(async () => await secondSessionTask);

            var updatedSaga = await GetById<TestSagaData>(saga.Id);
            Assert.That(updatedSaga.SagaProperty, Is.EqualTo("session 1 value"));
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
            public string SomeId { get; set; }

            public string SagaProperty { get; set; }
        }

        public class StartMessage
        {
            public string SomeId { get; set; }
        }

        public When_concurrent_update_exceed_lock_request_timeout_pessimistic(TestVariant param) : base(param)
        {
        }
    }
}