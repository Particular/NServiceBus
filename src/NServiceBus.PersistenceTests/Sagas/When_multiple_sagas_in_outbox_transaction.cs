namespace NServiceBus.PersistenceTesting.Sagas;

using NUnit.Framework;
using System.Threading.Tasks;
using System;

public class When_multiple_sagas_in_outbox_transaction : SagaPersisterTests
{
    [Test]
    public async Task Should_create_new_sagas_when_committed()
    {
        configuration.RequiresOutboxSupport();

        var saga1 = new Saga1Data { CorrelationId = Guid.NewGuid().ToString() };
        var saga2 = new Saga2Data { CorrelationId = Guid.NewGuid().ToString() };

        var context = configuration.GetContextBagForOutbox();
        var outboxMessage = await configuration.OutboxStorage.Get(Guid.NewGuid().ToString(), context);
        Assert.That(outboxMessage, Is.Null);
        using (var outboxTransaction = await configuration.OutboxStorage.BeginTransaction(context))
        {
            await using (var saga1Session = configuration.CreateStorageSession())
            {
                await saga1Session.TryOpen(outboxTransaction, context);
                var saga1Data = await configuration.SagaStorage.Get<Saga1Data>(nameof(Saga1Data.CorrelationId),
                    saga1.CorrelationId, saga1Session, context);
                Assert.That(saga1Data, Is.Null);

                await SaveSagaWithSession(saga1, saga1Session, context);

                await saga1Session.CompleteAsync();
            }

            await using (var saga2Session = configuration.CreateStorageSession())
            {
                await saga2Session.TryOpen(outboxTransaction, context);
                var saga2Data = await configuration.SagaStorage.Get<Saga2Data>(nameof(Saga2Data.CorrelationId),
                    saga2.CorrelationId, saga2Session, context);
                Assert.That(saga2Data, Is.Null);

                await SaveSagaWithSession(saga2, saga2Session, context);

                await saga2Session.CompleteAsync();
            }

            await outboxTransaction.Commit();
        }

        var sagaData1 = await GetById<Saga1Data>(saga1.Id);
        Assert.That(sagaData1, Is.Not.Null);
        Assert.That(sagaData1.CorrelationId, Is.EqualTo(saga1.CorrelationId));
        var sagaData2 = await GetById<Saga2Data>(saga2.Id);
        Assert.That(sagaData2, Is.Not.Null);
        Assert.That(sagaData2.CorrelationId, Is.EqualTo(saga2.CorrelationId));
    }

    [Test]
    public async Task Should_not_create_new_sagas_when_not_committed()
    {
        configuration.RequiresOutboxSupport();

        var saga1 = new Saga1Data { CorrelationId = Guid.NewGuid().ToString() };
        var saga2 = new Saga2Data { CorrelationId = Guid.NewGuid().ToString() };

        var context = configuration.GetContextBagForOutbox();
        var outboxMessage = await configuration.OutboxStorage.Get(Guid.NewGuid().ToString(), context);
        Assert.That(outboxMessage, Is.Null);
        using (var outboxTransaction = await configuration.OutboxStorage.BeginTransaction(context))
        {
            await using (var saga1Session = configuration.CreateStorageSession())
            {
                await saga1Session.TryOpen(outboxTransaction, context);
                var saga1Data = await configuration.SagaStorage.Get<Saga1Data>(nameof(Saga1Data.CorrelationId),
                    saga1.CorrelationId, saga1Session, context);
                Assert.That(saga1Data, Is.Null);

                await SaveSagaWithSession(saga1, saga1Session, context);

                await saga1Session.CompleteAsync();
            }

            await using (var saga2Session = configuration.CreateStorageSession())
            {
                await saga2Session.TryOpen(outboxTransaction, context);
                var saga2Data = await configuration.SagaStorage.Get<Saga2Data>(nameof(Saga2Data.CorrelationId),
                    saga2.CorrelationId, saga2Session, context);
                Assert.That(saga2Data, Is.Null);

                await SaveSagaWithSession(saga2, saga2Session, context);

                await saga2Session.CompleteAsync();
            }

            // no commit
        }

        var sagaData1 = await GetById<Saga1Data>(saga1.Id);
        Assert.That(sagaData1, Is.Null);
        var sagaData2 = await GetById<Saga2Data>(saga2.Id);
        Assert.That(sagaData2, Is.Null);
    }

    [Test]
    public async Task Should_update_existing_sagas_when_committed()
    {
        configuration.RequiresOutboxSupport();

        var saga1 = new Saga1Data { CorrelationId = Guid.NewGuid().ToString(), SomeSaga1Data = "saga 1 before update" };
        await SaveSaga(saga1);
        var saga2 = new Saga2Data { CorrelationId = Guid.NewGuid().ToString(), SomeSaga2Data = "saga 2 before update" };
        await SaveSaga(saga2);

        var context = configuration.GetContextBagForOutbox();
        var outboxMessage = await configuration.OutboxStorage.Get(Guid.NewGuid().ToString(), context);
        Assert.That(outboxMessage, Is.Null);
        using (var outboxTransaction = await configuration.OutboxStorage.BeginTransaction(context))
        {
            await using (var saga1Session = configuration.CreateStorageSession())
            {
                await saga1Session.TryOpen(outboxTransaction, context);

                var existingSaga1 = await configuration.SagaStorage.Get<Saga1Data>(saga1.Id, saga1Session, context);
                existingSaga1.SomeSaga1Data = "saga 1 after update";

                await configuration.SagaStorage.Update(existingSaga1, saga1Session, context);

                await saga1Session.CompleteAsync();
            }

            await using (var saga2Session = configuration.CreateStorageSession())
            {
                await saga2Session.TryOpen(outboxTransaction, context);

                var existingSaga2 = await configuration.SagaStorage.Get<Saga2Data>(saga2.Id, saga2Session, context);
                existingSaga2.SomeSaga2Data = "saga 2 after update";

                await configuration.SagaStorage.Update(existingSaga2, saga2Session, context);

                await saga2Session.CompleteAsync();
            }

            await outboxTransaction.Commit();
        }

        var saga1AfterUpdate = await GetById<Saga1Data>(saga1.Id);
        Assert.That(saga1AfterUpdate, Is.Not.Null);
        Assert.That(saga1AfterUpdate.SomeSaga1Data, Is.EqualTo("saga 1 after update"));
        var saga2AfterUpdate = await GetById<Saga2Data>(saga2.Id);
        Assert.That(saga2AfterUpdate, Is.Not.Null);
        Assert.That(saga2AfterUpdate.SomeSaga2Data, Is.EqualTo("saga 2 after update"));
    }

    [Test]
    public async Task Should_not_update_existing_sagas_when_not_committed()
    {
        configuration.RequiresOutboxSupport();

        var saga1 = new Saga1Data { CorrelationId = Guid.NewGuid().ToString(), SomeSaga1Data = "saga 1 before update" };
        await SaveSaga(saga1);
        var saga2 = new Saga2Data { CorrelationId = Guid.NewGuid().ToString(), SomeSaga2Data = "saga 2 before update" };
        await SaveSaga(saga2);

        var context = configuration.GetContextBagForOutbox();
        var outboxMessage = await configuration.OutboxStorage.Get(Guid.NewGuid().ToString(), context);
        Assert.That(outboxMessage, Is.Null);
        using (var outboxTransaction = await configuration.OutboxStorage.BeginTransaction(context))
        {
            await using (var saga1Session = configuration.CreateStorageSession())
            {
                await saga1Session.TryOpen(outboxTransaction, context);

                var existingSaga1 = await configuration.SagaStorage.Get<Saga1Data>(saga1.Id, saga1Session, context);
                existingSaga1.SomeSaga1Data = "saga 1 after update";

                await configuration.SagaStorage.Update(existingSaga1, saga1Session, context);

                await saga1Session.CompleteAsync();
            }

            await using (var saga2Session = configuration.CreateStorageSession())
            {
                await saga2Session.TryOpen(outboxTransaction, context);

                var existingSaga2 = await configuration.SagaStorage.Get<Saga2Data>(saga2.Id, saga2Session, context);
                existingSaga2.SomeSaga2Data = "saga 2 after update";

                await configuration.SagaStorage.Update(existingSaga2, saga2Session, context);

                await saga2Session.CompleteAsync();
            }

            // no commit
        }

        var saga1AfterUpdate = await GetById<Saga1Data>(saga1.Id);
        Assert.That(saga1AfterUpdate, Is.Not.Null);
        Assert.That(saga1AfterUpdate.SomeSaga1Data, Is.EqualTo("saga 1 before update"));
        var saga2AfterUpdate = await GetById<Saga2Data>(saga2.Id);
        Assert.That(saga2AfterUpdate, Is.Not.Null);
        Assert.That(saga2AfterUpdate.SomeSaga2Data, Is.EqualTo("saga 2 before update"));
    }

    public class Saga1 : Saga<Saga1Data>, IAmStartedByMessages<StartTestSagaMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga1Data> mapper) => mapper.ConfigureMapping<StartTestSagaMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationId);

        public Task Handle(StartTestSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();
    }

    public class Saga1Data : ContainSagaData
    {
        public string CorrelationId { get; set; }
        public string SomeSaga1Data { get; set; }
    }

    public class Saga2 : Saga<Saga2Data>, IAmStartedByMessages<StartTestSagaMessage>
    {
        protected override void ConfigureHowToFindSaga(SagaPropertyMapper<Saga2Data> mapper) => mapper.ConfigureMapping<StartTestSagaMessage>(m => m.CorrelationProperty).ToSaga(s => s.CorrelationId);

        public Task Handle(StartTestSagaMessage message, IMessageHandlerContext context) => throw new NotImplementedException();
    }

    public class Saga2Data : ContainSagaData
    {
        public string CorrelationId { get; set; }
        public string SomeSaga2Data { get; set; }
    }

    public class StartTestSagaMessage : IMessage
    {
        public string CorrelationProperty { get; set; }
    }

    public When_multiple_sagas_in_outbox_transaction(TestVariant param) : base(param)
    {
    }
}