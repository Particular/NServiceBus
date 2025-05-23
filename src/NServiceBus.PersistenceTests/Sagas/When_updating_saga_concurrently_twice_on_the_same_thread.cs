﻿namespace NServiceBus.PersistenceTesting.Sagas;

using System;
using System.Threading.Tasks;
using Extensibility;
using NUnit.Framework;
using Persistence;

public class When_updating_saga_concurrently_twice_on_the_same_thread : SagaPersisterTests
{
    [Test]
    public async Task Save_process_is_repeatable()
    {
        configuration.RequiresOptimisticConcurrencySupport();

        var correlationPropertyData = Guid.NewGuid().ToString();
        var saga = new TestSagaData { SomeId = correlationPropertyData, DateTimeProperty = DateTime.UtcNow };
        await SaveSaga(saga);

        ContextBag losingContext1;
        ICompletableSynchronizedStorageSession losingSaveSession1;
        TestSagaData staleRecord1;
        var persister = configuration.SagaStorage;

        var winningContext1 = configuration.GetContextBagForSagaStorage();
        var winningSaveSession1 = configuration.CreateStorageSession();
        await winningSaveSession1.Open(winningContext1);

        try
        {
            var record1 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession1, winningContext1);

            losingContext1 = configuration.GetContextBagForSagaStorage();
            losingSaveSession1 = configuration.CreateStorageSession();
            await losingSaveSession1.Open(losingContext1);

            staleRecord1 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession1, losingContext1);

            record1.DateTimeProperty = DateTime.UtcNow;
            await persister.Update(record1, winningSaveSession1, winningContext1);
            await winningSaveSession1.CompleteAsync();
        }
        finally
        {
            await winningSaveSession1.DisposeAsync();
        }

        try
        {
            staleRecord1.DateTimeProperty = DateTime.UtcNow.AddHours(1);
            Assert.That(async () =>
            {
                await persister.Update(staleRecord1, losingSaveSession1, losingContext1);
                await losingSaveSession1.CompleteAsync();
            }, Throws.InstanceOf<Exception>());
        }
        finally
        {
            await losingSaveSession1.DisposeAsync();
        }

        ContextBag losingContext2;
        ICompletableSynchronizedStorageSession losingSaveSession2;
        TestSagaData staleRecord2;

        var winningContext2 = configuration.GetContextBagForSagaStorage();
        var winningSaveSession2 = configuration.CreateStorageSession();
        await winningSaveSession2.Open(winningContext2);
        try
        {
            var record2 = await persister.Get<TestSagaData>(saga.Id, winningSaveSession2, winningContext2);

            losingContext2 = configuration.GetContextBagForSagaStorage();
            losingSaveSession2 = configuration.CreateStorageSession();
            await losingSaveSession2.Open(losingContext2);

            staleRecord2 = await persister.Get<TestSagaData>("SomeId", correlationPropertyData, losingSaveSession2, losingContext2);

            record2.DateTimeProperty = DateTime.UtcNow;
            await persister.Update(record2, winningSaveSession2, winningContext2);
            await winningSaveSession2.CompleteAsync();
        }
        finally
        {
            await winningSaveSession2.DisposeAsync();
        }

        try
        {
            staleRecord2.DateTimeProperty = DateTime.UtcNow.AddHours(1);
            Assert.That(async () =>
             {
                 await persister.Update(staleRecord2, losingSaveSession2, losingContext2);
                 await losingSaveSession2.CompleteAsync();
             }, Throws.InstanceOf<Exception>());
        }
        finally
        {
            await losingSaveSession2.DisposeAsync();
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

    public When_updating_saga_concurrently_twice_on_the_same_thread(TestVariant param) : base(param)
    {
    }
}