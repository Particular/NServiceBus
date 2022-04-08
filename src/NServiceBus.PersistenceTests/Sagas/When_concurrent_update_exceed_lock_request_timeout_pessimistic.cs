namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    public class When_concurrent_update_exceed_lock_request_timeout_pessimistic : SagaPersisterTests
    {
        public override async Task OneTimeSetUp()
        {
            if (!param.SessionTimeout.HasValue)
            {
                param.SessionTimeout = TimeSpan.FromMilliseconds(500);
            }
            configuration = new PersistenceTestsConfiguration(param);
            await configuration.Configure();
        }

        [Test]
        public async Task Should_fail_with_timeout()
        {
            configuration.RequiresPessimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData { SomeId = correlationPropertyData, SagaProperty = "initial value" };
            await SaveSaga(saga);

            var firstSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var persister = configuration.SagaStorage;

            async Task FirstSession()
            {
                var firstSessionContext = configuration.GetContextBagForSagaStorage();
                using (var firstSaveSession = configuration.CreateStorageSession())
                {
                    await firstSaveSession.OpenSession(firstSessionContext);

                    var record = await persister.Get<TestSagaData>(saga.Id, firstSaveSession, firstSessionContext);
                    firstSessionGetDone.SetResult(true);

                    var delayTime = configuration.SessionTimeout.GetValueOrDefault(TimeSpan.FromMilliseconds(1000));
                    await Task.Delay(delayTime.Add(delayTime)).ConfigureAwait(false);
                    await secondSessionGetDone.Task.ConfigureAwait(false);

                    record.SagaProperty = "session 1 value";
                    await persister.Update(record, firstSaveSession, firstSessionContext);
                    await firstSaveSession.CompleteAsync();
                }
            }

            async Task SecondSession()
            {
                var secondContext = configuration.GetContextBagForSagaStorage();
                using (var secondSession = configuration.CreateStorageSession())
                {
                    await secondSession.OpenSession(secondContext);

                    await firstSessionGetDone.Task.ConfigureAwait(false);

                    var recordTask = persister.Get<TestSagaData>(saga.Id, secondSession, secondContext);
                    secondSessionGetDone.SetResult(true);

                    var record = await recordTask.ConfigureAwait(false);
                    record.SagaProperty = "session 2 value";
                    await persister.Update(record, secondSession, secondContext);
                    await secondSession.CompleteAsync();
                }
            }

            var firstSessionTask = FirstSession();
            var secondSessionTask = SecondSession();

            Assert.DoesNotThrowAsync(async () => await firstSessionTask);
            Assert.CatchAsync<Exception>(async () => await secondSessionTask); // not all persisters guarantee a TimeoutException

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