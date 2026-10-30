#nullable enable

namespace NServiceBus;

using System;

static class SagaTimeoutManagementHeaders
{
    // Repeat values from the request body so behaviors that run before deserialization, such as the ones that
    // provide a partition key, can read them
    public const string SagaType = "NServiceBus.SagaTimeoutManagement.SagaType";
    public const string SagaId = "NServiceBus.SagaTimeoutManagement.SagaId";

    public const string RescheduleChange = "Reschedule";
    public const string CancelChange = "Cancel";
}

// A timeout change requested by an operator. It's a control message processed by the main receive pipeline,
// deserialized by ControlMessageSerializer regardless of the endpoint's serializer.
// A reschedule payload, when present, replaces the timeout payload as is, with its own content type.
sealed class SagaTimeoutChangeRequest
{
    public Guid OperationId { get; set; }
    public string SagaType { get; set; } = null!;
    public Guid SagaId { get; set; }
    public string TimeoutId { get; set; } = null!;
    public int ExpectedVersion { get; set; }
    public string Change { get; set; } = null!;
    public DateTimeOffset? DueTime { get; set; }
    public byte[]? Payload { get; set; }
    public string? PayloadContentType { get; set; }

    public bool IsReschedule => Change == SagaTimeoutManagementHeaders.RescheduleChange;

    public void Validate()
    {
        if (Change is not (SagaTimeoutManagementHeaders.RescheduleChange or SagaTimeoutManagementHeaders.CancelChange))
        {
            throw new InvalidSagaTimeoutManagementRequestException($"The change must be '{SagaTimeoutManagementHeaders.RescheduleChange}' or '{SagaTimeoutManagementHeaders.CancelChange}', but is '{Change}'.");
        }

        if (OperationId == Guid.Empty || SagaId == Guid.Empty || string.IsNullOrWhiteSpace(SagaType) || string.IsNullOrWhiteSpace(TimeoutId))
        {
            throw new InvalidSagaTimeoutManagementRequestException("The saga timeout change request must have an operation ID, a saga type, a saga ID, and a timeout ID.");
        }

        if (ExpectedVersion < 0)
        {
            throw new InvalidSagaTimeoutManagementRequestException($"The expected version {ExpectedVersion} isn't a valid version.");
        }

        if (IsReschedule && DueTime is null)
        {
            throw new InvalidSagaTimeoutManagementRequestException("A reschedule request must have a due time.");
        }

        if (Payload is { Length: > 0 })
        {
            if (!IsReschedule)
            {
                throw new InvalidSagaTimeoutManagementRequestException("Only a reschedule request can carry a new timeout payload.");
            }

            if (string.IsNullOrWhiteSpace(PayloadContentType))
            {
                throw new InvalidSagaTimeoutManagementRequestException("A new timeout payload must have a content type.");
            }
        }
    }
}

// Retrying a malformed request can't succeed. Thrown while deserializing, it becomes a MessageDeserializationException,
// which is unrecoverable, so the request goes straight to the error queue.
sealed class InvalidSagaTimeoutManagementRequestException(string message) : Exception(message);
