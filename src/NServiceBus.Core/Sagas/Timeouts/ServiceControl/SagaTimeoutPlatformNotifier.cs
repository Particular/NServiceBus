#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hosting;
using Sagas;
using ServicePlatform;

// Reports timeout changes to the ServiceControl primary instance as outgoing operations of the message being handled,
// so a rolled back saga save doesn't report changes that never happened.
sealed class SagaTimeoutPlatformNotifier(ServicePlatformConnection connection, string endpointName, HostInformation hostInformation, string managementAddress)
{
    public Task NotifyChanges(ActiveSagaInstance sagaInstance, IPipelineContext context) =>
        NotifyChanges(sagaInstance.Metadata, sagaInstance.Instance.Entity, sagaInstance.Timeouts.Changes, new PipelineReportSink(context), context.CancellationToken);

    public Task NotifyTriggered(ActiveSagaInstance sagaInstance, StoredSagaTimeout firedTimeout, IPipelineContext context) =>
        new PipelineReportSink(context).Send(triggered, Populate(new SagaTimeoutTriggered
        {
            EndpointName = endpointName,
            SagaType = SagaType(sagaInstance.Metadata),
            SagaId = sagaInstance.Instance.Entity.Id,
            ChangedAt = DateTimeOffset.UtcNow,
            Host = hostInformation.DisplayName,
            HostId = HostId,
            ManagementAddress = managementAddress,
            TimeoutId = firedTimeout.TimeoutId,
            TimeoutType = firedTimeout.MessageType,
            Version = firedTimeout.Version,
            TriggerId = firedTimeout.TriggerId
        }, sagaInstance.Metadata, sagaInstance.Instance.Entity));

    public Task NotifySagaCompleted(ActiveSagaInstance sagaInstance, IPipelineContext context) =>
        new PipelineReportSink(context).Send(completed, Populate(new SagaCompleted
        {
            EndpointName = endpointName,
            SagaType = SagaType(sagaInstance.Metadata),
            SagaId = sagaInstance.Instance.Entity.Id,
            ChangedAt = DateTimeOffset.UtcNow,
            Host = hostInformation.DisplayName,
            HostId = HostId,
            ManagementAddress = managementAddress
        }, sagaInstance.Metadata, sagaInstance.Instance.Entity));

    // Used when the saga can't be loaded, so there is no saga data to read the correlation property from
    public Task NotifyRejected(string sagaType, Guid sagaId, string timeoutId, string requestedChange, DateTimeOffset? requestedDueTime, string reason, Guid operationId, IPipelineContext context) =>
        new PipelineReportSink(context).Send(rejected, new SagaTimeoutChangeRejected
        {
            EndpointName = endpointName,
            SagaType = sagaType,
            SagaId = sagaId,
            ChangedAt = DateTimeOffset.UtcNow,
            Host = hostInformation.DisplayName,
            HostId = HostId,
            ManagementAddress = managementAddress,
            OperationId = operationId,
            TimeoutId = timeoutId,
            RequestedChange = requestedChange,
            RequestedDueTime = requestedDueTime,
            Reason = reason
        }, context.CancellationToken);

    public Task NotifyChanges(SagaMetadata metadata, IContainSagaData sagaData, IReadOnlyList<SagaTimeoutChange> changes, IPipelineContext context) =>
        NotifyChanges(metadata, sagaData, changes, new PipelineReportSink(context), context.CancellationToken);

    async Task NotifyChanges(SagaMetadata metadata, IContainSagaData sagaData, IReadOnlyList<SagaTimeoutChange> changes, ReportSink sink, CancellationToken cancellationToken)
    {
        foreach (var change in changes)
        {
            Task notification = change.Kind switch
            {
                SagaTimeoutChangeKind.Requested => sink.Send(requested, Populate(new SagaTimeoutRequested
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(metadata),
                    SagaId = sagaData.Id,
                    ChangedAt = change.ChangedAt,
                    Host = hostInformation.DisplayName,
                    HostId = HostId,
                    ManagementAddress = managementAddress,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version,
                    TriggerId = change.Timeout.TriggerId,
                    DueTime = change.Timeout.DueTime,
                    PayloadContentType = change.Timeout.ContentType,
                    Payload = change.Timeout.Payload
                }, metadata, sagaData), cancellationToken),
                SagaTimeoutChangeKind.Rescheduled => sink.Send(rescheduled, Populate(new SagaTimeoutRescheduled
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(metadata),
                    SagaId = sagaData.Id,
                    ChangedAt = change.ChangedAt,
                    Host = hostInformation.DisplayName,
                    HostId = HostId,
                    ManagementAddress = managementAddress,
                    OperationId = change.OperationId,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version,
                    TriggerId = change.Timeout.TriggerId,
                    DueTime = change.Timeout.DueTime,
                    PayloadContentType = change.Timeout.ContentType,
                    Payload = change.Timeout.Payload
                }, metadata, sagaData), cancellationToken),
                SagaTimeoutChangeKind.Canceled => sink.Send(canceled, Populate(new SagaTimeoutCanceled
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(metadata),
                    SagaId = sagaData.Id,
                    ChangedAt = change.ChangedAt,
                    Host = hostInformation.DisplayName,
                    HostId = HostId,
                    ManagementAddress = managementAddress,
                    OperationId = change.OperationId,
                    TimeoutId = change.TimeoutId,
                    TimeoutType = change.Timeout!.MessageType,
                    Version = change.Timeout.Version
                }, metadata, sagaData), cancellationToken),
                SagaTimeoutChangeKind.RescheduleRejected or SagaTimeoutChangeKind.CancelRejected => sink.Send(rejected, Populate(new SagaTimeoutChangeRejected
                {
                    EndpointName = endpointName,
                    SagaType = SagaType(metadata),
                    SagaId = sagaData.Id,
                    ChangedAt = change.ChangedAt,
                    Host = hostInformation.DisplayName,
                    HostId = HostId,
                    ManagementAddress = managementAddress,
                    OperationId = change.OperationId,
                    TimeoutId = change.TimeoutId,
                    RequestedChange = change.Kind == SagaTimeoutChangeKind.RescheduleRejected ? SagaTimeoutManagementHeaders.RescheduleChange : SagaTimeoutManagementHeaders.CancelChange,
                    RequestedDueTime = change.RequestedDueTime,
                    Reason = change.RejectionReason
                }, metadata, sagaData), cancellationToken),
                _ => throw new InvalidOperationException($"Unknown saga timeout change '{change.Kind}'.")
            };

            await notification.ConfigureAwait(false);
        }
    }

    static string SagaType(SagaMetadata metadata) => metadata.SagaType.FullName!;

    static TMessage Populate<TMessage>(TMessage message, SagaMetadata metadata, IContainSagaData sagaData) where TMessage : SagaTimeoutPlatformMessage
    {
        if (metadata.TryGetCorrelationProperty(out var correlationProperty))
        {
            message.CorrelationPropertyValue = correlationProperty.Accessor.AccessFrom(sagaData)?.ToString();
        }

        return message;
    }

    string HostId => hostInformation.HostId.ToString("N");

    readonly ServicePlatformSender<SagaTimeoutRequested> requested = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutRequested);
    readonly ServicePlatformSender<SagaTimeoutRescheduled> rescheduled = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutRescheduled);
    readonly ServicePlatformSender<SagaTimeoutCanceled> canceled = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutCanceled);
    readonly ServicePlatformSender<SagaTimeoutTriggered> triggered = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutTriggered);
    readonly ServicePlatformSender<SagaTimeoutChangeRejected> rejected = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaTimeoutChangeRejected);
    readonly ServicePlatformSender<SagaCompleted> completed = connection.PrimaryInstance.CreateSender(SagaTimeoutPlatformMessagesJsonContext.Default.SagaCompleted);

    abstract class ReportSink
    {
        public abstract Task Send<TMessage>(ServicePlatformSender<TMessage> sender, TMessage message, CancellationToken cancellationToken = default);
    }

    sealed class PipelineReportSink(IPipelineContext context) : ReportSink
    {
        // The pipeline context carries its own cancellation token
        public override Task Send<TMessage>(ServicePlatformSender<TMessage> sender, TMessage message, CancellationToken cancellationToken = default) => sender.Send(message, context);
    }
}

