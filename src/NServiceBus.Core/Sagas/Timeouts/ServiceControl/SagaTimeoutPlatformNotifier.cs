#nullable enable

namespace NServiceBus;

using System;
using System.Threading.Tasks;
using Sagas;
using ServicePlatform;

// Reports timeout changes to the ServiceControl primary instance as outgoing operations of the message being handled,
// so a rolled back saga save doesn't report changes that never happened.
sealed class SagaTimeoutPlatformNotifier(ServicePlatformConnection connection, string endpointName)
{
    public async Task NotifyChanges(ActiveSagaInstance sagaInstance, IPipelineContext context)
    {
        foreach (var change in sagaInstance.Timeouts.Changes)
        {
            Task notification = change.Kind switch
            {
                SagaTimeoutChangeKind.Requested => requested.Send(Populate(new SagaTimeoutRequested
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(sagaInstance),
                    SagaId = sagaInstance.Instance.Entity.Id,
                    ChangedAt = change.ChangedAt,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version,
                    TriggerId = change.Timeout.TriggerId,
                    DueTime = change.Timeout.DueTime,
                    PayloadContentType = change.Timeout.ContentType,
                    Payload = change.Timeout.Payload
                }, sagaInstance), context),
                SagaTimeoutChangeKind.Rescheduled => rescheduled.Send(Populate(new SagaTimeoutRescheduled
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(sagaInstance),
                    SagaId = sagaInstance.Instance.Entity.Id,
                    ChangedAt = change.ChangedAt,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version,
                    TriggerId = change.Timeout.TriggerId,
                    DueTime = change.Timeout.DueTime,
                    PayloadContentType = change.Timeout.ContentType,
                    Payload = change.Timeout.Payload
                }, sagaInstance), context),
                SagaTimeoutChangeKind.Canceled => canceled.Send(Populate(new SagaTimeoutCanceled
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(sagaInstance),
                    SagaId = sagaInstance.Instance.Entity.Id,
                    ChangedAt = change.ChangedAt,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version
                }, sagaInstance), context),
                SagaTimeoutChangeKind.RescheduleRejected or SagaTimeoutChangeKind.CancelRejected => rejected.Send(Populate(new SagaTimeoutChangeRejected
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(sagaInstance),
                    SagaId = sagaInstance.Instance.Entity.Id,
                    ChangedAt = change.ChangedAt,
                    TimeoutId = change.TimeoutId,
                    RequestedChange = change.Kind == SagaTimeoutChangeKind.RescheduleRejected ? "Reschedule" : "Cancel",
                    RequestedDueTime = change.RequestedDueTime,
                    // Fired and canceled timeouts are removed from storage, so the endpoint can't tell them apart from an unknown ID
                    Reason = "TimeoutNotFound"
                }, sagaInstance), context),
                _ => throw new InvalidOperationException($"Unknown saga timeout change '{change.Kind}'.")
            };

            await notification.ConfigureAwait(false);
        }
    }

    public Task NotifyTriggered(ActiveSagaInstance sagaInstance, StoredSagaTimeout firedTimeout, IPipelineContext context) =>
        triggered.Send(Populate(new SagaTimeoutTriggered
        {
            EndpointName = endpointName,
            SagaType = SagaType(sagaInstance),
            SagaId = sagaInstance.Instance.Entity.Id,
            ChangedAt = DateTimeOffset.UtcNow,
            TimeoutId = firedTimeout.TimeoutId,
            TimeoutType = firedTimeout.MessageType,
            Version = firedTimeout.Version,
            TriggerId = firedTimeout.TriggerId
        }, sagaInstance), context);

    public Task NotifySagaCompleted(ActiveSagaInstance sagaInstance, IPipelineContext context) =>
        completed.Send(Populate(new SagaCompleted
        {
            EndpointName = endpointName,
            SagaType = SagaType(sagaInstance),
            SagaId = sagaInstance.Instance.Entity.Id,
            ChangedAt = DateTimeOffset.UtcNow
        }, sagaInstance), context);

    static string SagaType(ActiveSagaInstance sagaInstance) => sagaInstance.Metadata.SagaType.FullName!;

    static TMessage Populate<TMessage>(TMessage message, ActiveSagaInstance sagaInstance) where TMessage : SagaTimeoutPlatformMessage
    {
        if (sagaInstance.Metadata.TryGetCorrelationProperty(out var correlationProperty))
        {
            message.CorrelationPropertyValue = correlationProperty.Accessor.AccessFrom(sagaInstance.Instance.Entity)?.ToString();
        }

        return message;
    }

    readonly ServicePlatformSender<SagaTimeoutRequested> requested = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutRequested);
    readonly ServicePlatformSender<SagaTimeoutRescheduled> rescheduled = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutRescheduled);
    readonly ServicePlatformSender<SagaTimeoutCanceled> canceled = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutCanceled);
    readonly ServicePlatformSender<SagaTimeoutTriggered> triggered = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutTriggered);
    readonly ServicePlatformSender<SagaTimeoutChangeRejected> rejected = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutChangeRejected);
    readonly ServicePlatformSender<SagaCompleted> completed = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaCompleted);
}
