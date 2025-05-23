﻿namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using Extensibility;
using NUnit.Framework;
using Persistence;

public class When_worker_tries_to_complete_saga_update_by_another_optimistic : SagaPersisterTests
{
    [Test]
    public async Task Should_fail()
    {
        configuration.RequiresOptimisticConcurrencySupport();

        var correlationPropertyData = Guid.NewGuid().ToString();
        var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };

        await SaveSaga(saga);

        var persister = configuration.SagaStorage;

        ContextBag losingContext;
        ICompletableSynchronizedStorageSession losingSaveSession;
        TestSagaData staleRecord;

        var winningContext = configuration.GetContextBagForSagaStorage();
        var winningSaveSession = configuration.CreateStorageSession();
        await winningSaveSession.Open(winningContext);
        try
        {
            var record = await persister.Get<TestSagaData>(saga.Id, winningSaveSession, winningContext);

            losingContext = configuration.GetContextBagForSagaStorage();
            losingSaveSession = configuration.CreateStorageSession();
            await losingSaveSession.Open(losingContext);
            staleRecord = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession, losingContext);

            record.DateTimeProperty = DateTime.UtcNow;
            await persister.Update(record, winningSaveSession, winningContext);
            await winningSaveSession.CompleteAsync();
        }
        finally
        {
            await winningSaveSession.DisposeAsync();
        }

        try
        {
            Assert.That(async () =>
            {
                await persister.Complete(staleRecord, losingSaveSession, losingContext);
                await losingSaveSession.CompleteAsync();
            }, Throws.InstanceOf<Exception>());
        }
        finally
        {
            await losingSaveSession.DisposeAsync();
        }
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

    public When_worker_tries_to_complete_saga_update_by_another_optimistic(TestVariant param) : base(param)
    {
    }
}