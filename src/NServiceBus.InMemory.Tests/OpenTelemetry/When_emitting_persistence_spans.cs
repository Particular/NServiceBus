namespace NServiceBus.Persistence.InMemory;

using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Extensibility;
using NServiceBus.Outbox;
using NServiceBus.Sagas;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;
using NUnit.Framework;

[TestFixture]
public class When_emitting_persistence_spans
{
    [Test]
    public async Task Should_create_saga_spans_and_transaction_events()
    {
        var storage = new InMemoryStorage();
        var persister = new InMemorySagaPersister(storage, new InMemorySagaPersisterSettings(new System.Text.Json.JsonSerializerOptions()));
        using var listener = new TestingActivityListener(InMemoryPersistenceTracing.ActivitySourceName);
        using var root = StartRootActivity();
        var session = new InMemorySynchronizedStorageSession();
        var context = new ContextBag();
        var sagaData = new TestSagaData { Id = Guid.NewGuid(), CorrelationId = Guid.NewGuid(), Value = "first" };
        var correlation = new SagaCorrelationProperty(nameof(TestSagaData.CorrelationId), sagaData.CorrelationId);

        await session.Open(context);
        await persister.Save(sagaData, correlation, session, context);
        await session.CompleteAsync();
        var loadedSaga = await persister.Get<TestSagaData>(sagaData.Id, session, context);

        Assert.That(loadedSaga, Is.Not.Null);

        sagaData.Value = "second";
        await session.Open(context);
        await persister.Get<TestSagaData>(sagaData.Id, session, context);
        await persister.Update(sagaData, session, context);
        await session.CompleteAsync();

        await session.Open(context);
        await persister.Get<TestSagaData>(sagaData.Id, session, context);
        await persister.Complete(sagaData, session, context);
        await session.CompleteAsync();
        root.Stop();

        var activities = listener.CompletedFrom(InMemoryPersistenceTracing.ActivitySourceName);

        Assert.Multiple(() =>
        {
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SagaSaveActivityName), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SagaGetByIdActivityName && Equals(a.GetTagItem("nservicebus.persistence.result"), "hit")), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SagaUpdateActivityName), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SagaCompleteActivityName), Is.True);
            Assert.That(activities.Any(a => a.Events.Any(e => e.Name == "inmemory.persistence.transaction.enlisted")), Is.True);
            Assert.That(root.Events.Any(e => e.Name == "inmemory.persistence.transaction.committed"), Is.True);
        });
    }

    [Test]
    public async Task Should_create_outbox_spans()
    {
        var storage = new InMemoryStorage();
        var outbox = new InMemoryOutboxStorage(storage);
        using var listener = new TestingActivityListener(InMemoryPersistenceTracing.ActivitySourceName);
        using var root = StartRootActivity();
        var context = new ContextBag();
        await using var transaction = await outbox.BeginTransaction(context);
        var outboxMessage = new OutboxMessage("message-id", []);

        await outbox.Store(outboxMessage, transaction, context);
        await transaction.Commit();
        await outbox.Get("message-id", context);
        await outbox.SetAsDispatched("message-id", context);
        root.Stop();

        var activities = listener.CompletedFrom(InMemoryPersistenceTracing.ActivitySourceName);

        Assert.Multiple(() =>
        {
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.OutboxBeginTransactionActivityName), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.OutboxStoreActivityName), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.OutboxGetActivityName && Equals(a.GetTagItem("nservicebus.persistence.result"), "hit")), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.OutboxSetAsDispatchedActivityName && a.Events.Any(e => e.Name == "inmemory.persistence.marked_dispatched")), Is.True);
        });
    }

    [Test]
    public async Task Should_create_subscription_spans()
    {
        var storage = new InMemoryStorage();
        var subscriptionStorage = new SubscriptionStorage.InMemorySubscriptionStorage(storage);
        using var listener = new TestingActivityListener(InMemoryPersistenceTracing.ActivitySourceName);
        using var root = StartRootActivity();
        var context = new ContextBag();
        var messageType = new MessageType(typeof(TestEvent));
        var subscriber = new Subscriber("endpoint-a", "EndpointA");

        await subscriptionStorage.Subscribe(subscriber, messageType, context);
        var subscribers = await subscriptionStorage.GetSubscriberAddressesForMessage([messageType], context);
        await subscriptionStorage.Unsubscribe(subscriber, messageType, context);
        root.Stop();

        var activities = listener.CompletedFrom(InMemoryPersistenceTracing.ActivitySourceName);

        Assert.Multiple(() =>
        {
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SubscriptionSubscribeActivityName), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SubscriptionGetSubscribersActivityName && a.Events.Any(e => e.Name == "inmemory.persistence.resolved_subscribers")), Is.True);
            Assert.That(activities.Any(a => a.OperationName == InMemoryPersistenceTracing.SubscriptionUnsubscribeActivityName), Is.True);
            Assert.That(subscribers.Single().TransportAddress, Is.EqualTo("endpoint-a"));
        });
    }

    [Test]
    public void Should_emit_rollback_event_when_storage_transaction_fails()
    {
        var transaction = new InMemoryStorageTransaction();
        using var root = StartRootActivity();

        transaction.Enlist(new object(), static _ => { });
        transaction.Enlist(new InvalidOperationException("boom"), static state => throw state);

        Assert.Throws<InvalidOperationException>(() => transaction.Commit());

        root.Stop();

        Assert.That(root.Events.Any(e => e.Name == "inmemory.persistence.transaction.rolled_back"), Is.True);
    }

    static Activity StartRootActivity()
    {
        var activity = new Activity("root");
        activity.SetIdFormat(ActivityIdFormat.W3C);
        activity.Start();
        return activity;
    }

    public class TestSagaData : ContainSagaData
    {
        public Guid CorrelationId { get; set; }

        public string Value { get; set; }
    }

    public class TestEvent : IEvent;
}
