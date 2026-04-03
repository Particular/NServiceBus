namespace NServiceBus.Persistence.InMemory;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NServiceBus.Unicast.Subscriptions;
using NServiceBus.Unicast.Subscriptions.MessageDrivenSubscriptions;

static class InMemoryPersistenceTracing
{
    public const string ActivitySourceName = "NServiceBus.InMemory";

    public const string SagaGetByIdActivityName = "NServiceBus.InMemory.Persistence.Saga.GetById";
    public const string SagaGetByPropertyActivityName = "NServiceBus.InMemory.Persistence.Saga.GetByProperty";
    public const string SagaSaveActivityName = "NServiceBus.InMemory.Persistence.Saga.Save";
    public const string SagaUpdateActivityName = "NServiceBus.InMemory.Persistence.Saga.Update";
    public const string SagaCompleteActivityName = "NServiceBus.InMemory.Persistence.Saga.Complete";

    public const string OutboxBeginTransactionActivityName = "NServiceBus.InMemory.Persistence.Outbox.BeginTransaction";
    public const string OutboxGetActivityName = "NServiceBus.InMemory.Persistence.Outbox.Get";
    public const string OutboxStoreActivityName = "NServiceBus.InMemory.Persistence.Outbox.Store";
    public const string OutboxSetAsDispatchedActivityName = "NServiceBus.InMemory.Persistence.Outbox.SetAsDispatched";

    public const string SubscriptionSubscribeActivityName = "NServiceBus.InMemory.Persistence.Subscription.Subscribe";
    public const string SubscriptionUnsubscribeActivityName = "NServiceBus.InMemory.Persistence.Subscription.Unsubscribe";
    public const string SubscriptionGetSubscribersActivityName = "NServiceBus.InMemory.Persistence.Subscription.GetSubscribers";

    const string PersistenceStorageTag = "nservicebus.persistence.storage";
    const string PersistenceTypeTag = "nservicebus.persistence.type";
    const string PersistenceOperationTag = "nservicebus.persistence.operation";
    const string PersistenceEntityTag = "nservicebus.persistence.entity";
    const string LookupTag = "nservicebus.persistence.lookup";
    const string SagaIdTag = "nservicebus.persistence.saga_id";
    const string OutboxMessageIdTag = "nservicebus.persistence.outbox_message_id";
    const string SubscriberTransportAddressTag = "nservicebus.persistence.subscriber.transport_address";
    const string SubscriberEndpointTag = "nservicebus.persistence.subscriber.endpoint";
    const string MessageTypeTag = "nservicebus.persistence.message_type";
    const string ResultTag = "nservicebus.persistence.result";
    const string CountTag = "nservicebus.persistence.count";
    const string ErrorTypeTag = "error.type";

    const string StorageName = "inmemory";
    const string SagaType = "saga";
    const string OutboxType = "outbox";
    const string SubscriptionType = "subscription";

    const string TransactionEnlistedEvent = "inmemory.persistence.transaction.enlisted";
    const string TransactionCommittedEvent = "inmemory.persistence.transaction.committed";
    const string TransactionRolledBackEvent = "inmemory.persistence.transaction.rolled_back";
    const string StagedEvent = "inmemory.persistence.staged";
    const string MissEvent = "inmemory.persistence.miss";
    const string HitEvent = "inmemory.persistence.hit";
    const string MarkedDispatchedEvent = "inmemory.persistence.marked_dispatched";
    const string ResolvedSubscribersEvent = "inmemory.persistence.resolved_subscribers";

    static readonly ActivitySource activitySource = new(ActivitySourceName, "0.1.0");

    public static bool HasListeners() => activitySource.HasListeners();

    public static Activity? StartSagaGetById(Guid sagaId) => StartActivity(
        SagaGetByIdActivityName,
        SagaType,
        "get",
        "saga",
        new TagList
        {
            { LookupTag, "id" },
            { SagaIdTag, sagaId.ToString() }
        });

    public static Activity? StartSagaGetByProperty(Type sagaType, string propertyName, object propertyValue) => StartActivity(
        SagaGetByPropertyActivityName,
        SagaType,
        "get",
        "saga",
        new TagList
        {
            { LookupTag, propertyName },
            { MessageTypeTag, sagaType.FullName },
            { ResultTag, propertyValue?.ToString() }
        });

    public static Activity? StartSagaSave(Guid sagaId) => StartActivity(
        SagaSaveActivityName,
        SagaType,
        "save",
        "saga",
        new TagList { { SagaIdTag, sagaId.ToString() } });

    public static Activity? StartSagaUpdate(Guid sagaId) => StartActivity(
        SagaUpdateActivityName,
        SagaType,
        "update",
        "saga",
        new TagList { { SagaIdTag, sagaId.ToString() } });

    public static Activity? StartSagaComplete(Guid sagaId) => StartActivity(
        SagaCompleteActivityName,
        SagaType,
        "complete",
        "saga",
        new TagList { { SagaIdTag, sagaId.ToString() } });

    public static Activity? StartOutboxBeginTransaction() => StartActivity(
        OutboxBeginTransactionActivityName,
        OutboxType,
        "begin_transaction",
        "outbox",
        []);

    public static Activity? StartOutboxGet(string messageId) => StartActivity(
        OutboxGetActivityName,
        OutboxType,
        "get",
        "outbox",
        new TagList { { OutboxMessageIdTag, messageId } });

    public static Activity? StartOutboxStore(string messageId, int operationCount) => StartActivity(
        OutboxStoreActivityName,
        OutboxType,
        "store",
        "outbox",
        new TagList
        {
            { OutboxMessageIdTag, messageId },
            { CountTag, operationCount }
        });

    public static Activity? StartOutboxSetAsDispatched(string messageId) => StartActivity(
        OutboxSetAsDispatchedActivityName,
        OutboxType,
        "set_dispatched",
        "outbox",
        new TagList { { OutboxMessageIdTag, messageId } });

    public static Activity? StartSubscriptionSubscribe(Subscriber subscriber, MessageType messageType) => StartActivity(
        SubscriptionSubscribeActivityName,
        SubscriptionType,
        "subscribe",
        "subscription",
        CreateSubscriptionTags(subscriber, messageType));

    public static Activity? StartSubscriptionUnsubscribe(Subscriber subscriber, MessageType messageType) => StartActivity(
        SubscriptionUnsubscribeActivityName,
        SubscriptionType,
        "unsubscribe",
        "subscription",
        CreateSubscriptionTags(subscriber, messageType));

    public static Activity? StartGetSubscribers(IEnumerable<MessageType> messageTypes) => StartActivity(
        SubscriptionGetSubscribersActivityName,
        SubscriptionType,
        "get_subscribers",
        "subscription",
        new TagList { { CountTag, messageTypes.Count() } });

    public static void AddTransactionEnlistedEvent(string operationType) =>
        Activity.Current?.AddEvent(new ActivityEvent(TransactionEnlistedEvent, tags: new ActivityTagsCollection
        {
            [PersistenceTypeTag] = operationType
        }));

    public static void AddTransactionCommittedEvent(int operationCount) =>
        Activity.Current?.AddEvent(new ActivityEvent(TransactionCommittedEvent, tags: new ActivityTagsCollection
        {
            [CountTag] = operationCount
        }));

    public static void AddTransactionRolledBackEvent(int operationCount) =>
        Activity.Current?.AddEvent(new ActivityEvent(TransactionRolledBackEvent, tags: new ActivityTagsCollection
        {
            [CountTag] = operationCount
        }));

    public static void AddStagedEvent(Activity? activity, int? count = null)
    {
        if (activity == null)
        {
            return;
        }

        var tags = count.HasValue
            ? new ActivityTagsCollection { [CountTag] = count.Value }
            : null;

        activity.AddEvent(new ActivityEvent(StagedEvent, tags: tags));
    }

    public static void AddHitEvent(Activity? activity)
    {
        activity?.SetTag(ResultTag, "hit");
        activity?.AddEvent(new ActivityEvent(HitEvent));
    }

    public static void AddMissEvent(Activity? activity)
    {
        activity?.SetTag(ResultTag, "miss");
        activity?.AddEvent(new ActivityEvent(MissEvent));
    }

    public static void AddMarkedDispatchedEvent(Activity? activity) =>
        activity?.AddEvent(new ActivityEvent(MarkedDispatchedEvent));

    public static void AddResolvedSubscribersEvent(Activity? activity, int subscriberCount)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetTag(CountTag, subscriberCount);
        activity.AddEvent(new ActivityEvent(ResolvedSubscribersEvent, tags: new ActivityTagsCollection
        {
            [CountTag] = subscriberCount
        }));
    }

    public static void MarkSuccess(Activity? activity)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Ok);
    }

    public static void MarkError(Activity? activity, Exception ex)
    {
        if (activity == null)
        {
            return;
        }

        activity.SetStatus(ActivityStatusCode.Error, ex.Message);
        activity.SetTag("otel.status_code", "ERROR");
        activity.SetTag("otel.status_description", ex.Message);
        activity.SetTag(ErrorTypeTag, ex.GetType().Name);
        activity.AddEvent(new ActivityEvent("exception", DateTimeOffset.UtcNow,
        [
            new KeyValuePair<string, object?>("exception.escaped", true),
            new KeyValuePair<string, object?>("exception.type", ex.GetType()),
            new KeyValuePair<string, object?>("exception.message", ex.Message),
            new KeyValuePair<string, object?>("exception.stacktrace", ex.ToString())
        ]));
    }

    static Activity? StartActivity(string activityName, string persistenceType, string operation, string entity, TagList additionalTags)
    {
        var tags = new TagList
        {
            { PersistenceStorageTag, StorageName },
            { PersistenceTypeTag, persistenceType },
            { PersistenceOperationTag, operation },
            { PersistenceEntityTag, entity }
        };

        foreach (var tag in additionalTags)
        {
            tags.Add(tag.Key, tag.Value);
        }

        var activity = activitySource.CreateActivity(activityName, ActivityKind.Internal, Activity.Current?.Context ?? default, tags, links: null, idFormat: ActivityIdFormat.W3C);
        if (activity == null)
        {
            return null;
        }

        activity.DisplayName = operation;
        activity.Start();
        return activity;
    }

    static TagList CreateSubscriptionTags(Subscriber subscriber, MessageType messageType) =>
        new()
        {
            { SubscriberTransportAddressTag, subscriber.TransportAddress },
            { SubscriberEndpointTag, subscriber.Endpoint },
            { MessageTypeTag, messageType.TypeName }
        };
}
