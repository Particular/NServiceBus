#nullable enable

namespace NServiceBus;

using System;
using System.Text.Json.Serialization;

// Sent to the ServiceControl primary instance, which keeps the active timeouts list and the timeout history.
abstract class SagaTimeoutPlatformMessage
{
    public required string EndpointName { get; set; }
    public required string SagaType { get; set; }
    public required Guid SagaId { get; set; }
    public string? CorrelationPropertyValue { get; set; }
    // Time on the machine that made the change, so the platform can account for clock drift between machines
    public required DateTimeOffset ChangedAt { get; set; }
}

abstract class SagaTimeoutChangeMessage : SagaTimeoutPlatformMessage
{
    public required string TimeoutId { get; set; }
    public required string TimeoutType { get; set; }
    public required int Version { get; set; }
}

sealed class SagaTimeoutRequested : SagaTimeoutChangeMessage
{
    public required string TriggerId { get; set; }
    public required DateTimeOffset DueTime { get; set; }
    public required string PayloadContentType { get; set; }
    public required byte[] Payload { get; set; }
}

sealed class SagaTimeoutRescheduled : SagaTimeoutChangeMessage
{
    public required string TriggerId { get; set; }
    public required DateTimeOffset DueTime { get; set; }
    public required string PayloadContentType { get; set; }
    public required byte[] Payload { get; set; }
}

sealed class SagaTimeoutCanceled : SagaTimeoutChangeMessage;

sealed class SagaTimeoutTriggered : SagaTimeoutChangeMessage
{
    public required string TriggerId { get; set; }
}

sealed class SagaTimeoutChangeRejected : SagaTimeoutPlatformMessage
{
    public required string TimeoutId { get; set; }
    public required string RequestedChange { get; set; }
    public DateTimeOffset? RequestedDueTime { get; set; }
    public required string Reason { get; set; }
}

// All pending timeouts of the saga are discarded with it
sealed class SagaCompleted : SagaTimeoutPlatformMessage;

[JsonSerializable(typeof(SagaTimeoutRequested))]
[JsonSerializable(typeof(SagaTimeoutRescheduled))]
[JsonSerializable(typeof(SagaTimeoutCanceled))]
[JsonSerializable(typeof(SagaTimeoutTriggered))]
[JsonSerializable(typeof(SagaTimeoutChangeRejected))]
[JsonSerializable(typeof(SagaCompleted))]
sealed partial class SagaTimeoutPlatformMessagesJsonContext : JsonSerializerContext;
