namespace NServiceBus.PersistenceTesting.Sagas
{
    using System;
    using System.Threading.Tasks;
    using NUnit.Framework;

    [TestFixture]
    public class When_worker_tries_to_complete_saga_update_by_another_pessimistic : SagaPersisterTests<When_worker_tries_to_complete_saga_update_by_another_pessimistic.TestSaga, When_worker_tries_to_complete_saga_update_by_another_pessimistic.TestSagaData>
    {
        [Test]
        public async Task Should_complete()
        {
            configuration.RequiresPessimisticConcurrencySupport();

            var correlationPropertyData = Guid.NewGuid().ToString();
            var saga = new TestSagaData {SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow};
            await SaveSaga(saga);

            var firstSessionDateTimeValue = DateTime.UtcNow.AddDays(-2);
            var secondSessionDateTimeValue = DateTime.UtcNow.AddDays(-1);

            var firstSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var secondSessionGetDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var persister = configuration.SagaStorage;

            async Task FirstSession()
            {
                var firstContent = configuration.GetContextBagForSagaStorage();
                var firstSaveSession = await configuration.SynchronizedStorage.OpenSession(firstContent);
                try
                {
                    var record = await persister.Get<TestSagaData>(saga.Id, firstSaveSession, firstContent);
                    firstSessionGetDone.SetResult(true);

                    record.DateTimeProperty = firstSessionDateTimeValue;
                    await persister.Update(record, firstSaveSession, firstContent);
                    await secondSessionGetDone.Task.ConfigureAwait(false);
                    await firstSaveSession.CompleteAsync();
                }
                finally
                {
                    firstSaveSession.Dispose();
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
                    record.DateTimeProperty = secondSessionDateTimeValue;
                    await persister.Update(record, secondSession, secondContext);
                    await secondSession.CompleteAsync();
                }
                finally
                {
                    secondSession.Dispose();
                }
            }

            await Task.WhenAll(SecondSession(), FirstSession());

            var result = await GetByCorrelationProperty(nameof(TestSagaData.SomeId), correlationPropertyData);

            // MongoDB stores datetime with less precision
            Assert.That(result.DateTimeProperty, Is.EqualTo(secondSessionDateTimeValue).Within(TimeSpan.FromMilliseconds(1)));
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

            public class StartMessage
            {
                public string SomeId { get; set; }
            }

            public class TestSagaData : ContainSagaData
            {
                public string SomeId { get; set; } = "Test";

                public DateTime DateTimeProperty { get; set; }
            }
    }
}