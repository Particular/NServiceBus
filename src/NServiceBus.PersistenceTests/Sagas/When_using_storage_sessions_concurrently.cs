namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Persistence;

public class When_using_storage_sessions_concurrently(TestVariant param) : SagaPersisterTests(param)
{
    [Test]
    public void It_should_not_throw_when_being_disposed_more_than_once() =>
        // 1 : Open
        // 1 : Commit
        // 1 : Dispose
        // 2 : Open
        // 1 : Dispose
        // 2 : Commit
        Assert.DoesNotThrowAsync(async () =>
        {
            var correlationPropertyData = Guid.NewGuid().ToString();
            var sagaData = new TestSagaData
            {
                SomeId = correlationPropertyData,
                DateTimeProperty = DateTime.UtcNow
            };
            await SaveSaga(sagaData);
            var generatedSagaId = sagaData.Id;

            var persister = configuration.SagaStorage;

            //  1: Open
            var session1Context = configuration.GetContextBagForSagaStorage();

            ICompletableSynchronizedStorageSession session1StorageSession;

            using (session1StorageSession = configuration.CreateStorageSession())
            {
                await session1StorageSession.Open(session1Context);

                var record = await persister.Get<TestSagaData>(generatedSagaId, session1StorageSession, session1Context);

                record.DateTimeProperty = DateTime.UtcNow;

                await persister.Update(record, session1StorageSession, session1Context);
                // 1:  Commit
                await session1StorageSession.CompleteAsync();
            } //dispose session1StorageSession


            var session2Context = configuration.GetContextBagForSagaStorage();

            using (var session2StorageSession = configuration.CreateStorageSession())
            {
                // 2: Open
                await session2StorageSession.Open(session2Context);

                // 1: Dispose:
                session1StorageSession.Dispose();

                var staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData,
                    session2StorageSession, session2Context);
                staleRecord.DateTimeProperty = DateTime.UtcNow.AddHours(1);

                // 2: Commit
                await persister.Update(staleRecord, session2StorageSession, session2Context);
                await session2StorageSession.CompleteAsync();
            } //dispose session2StorageSession 
        });

    // Needed to satisfy
    public class TestSaga : Saga<TestSagaData>, IAmStartedByMessages<StartMessage>
    {
        public Task Handle(StartMessage message, IMessageHandlerContext context) => throw new NotSupportedException();

        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<TestSagaData> mapper) => mapper.MapSaga(s => s.SomeId).ToMessage<StartMessage>(msg => msg.SomeId);
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