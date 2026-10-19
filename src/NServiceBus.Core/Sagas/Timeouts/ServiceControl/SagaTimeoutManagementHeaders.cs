#nullable enable

namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Globalization;

// A timeout change requested by an operator travels in headers, so the satellite never deserializes the body.
// A reschedule body, when present, replaces the timeout payload as is, with the content type from the ContentType header.
static class SagaTimeoutManagementHeaders
{
    public const string OperationId = "NServiceBus.SagaTimeoutManagement.OperationId";
    public const string SagaType = "NServiceBus.SagaTimeoutManagement.SagaType";
    public const string SagaId = "NServiceBus.SagaTimeoutManagement.SagaId";
    public const string TimeoutId = "NServiceBus.SagaTimeoutManagement.TimeoutId";
    public const string ExpectedVersion = "NServiceBus.SagaTimeoutManagement.ExpectedVersion";
    public const string Change = "NServiceBus.SagaTimeoutManagement.Change";
    public const string DueTime = "NServiceBus.SagaTimeoutManagement.DueTime";

    public const string RescheduleChange = "Reschedule";
    public const string CancelChange = "Cancel";
}

sealed record SagaTimeoutManagementRequest(Guid OperationId, string SagaType, Guid SagaId, string TimeoutId, int ExpectedVersion, string Change, DateTimeOffset? DueTime, string? PayloadContentType)
{
    public bool IsReschedule => Change == SagaTimeoutManagementHeaders.RescheduleChange;

    public static SagaTimeoutManagementRequest Parse(IReadOnlyDictionary<string, string> headers, ReadOnlyMemory<byte> body)
    {
        var change = Required(headers, SagaTimeoutManagementHeaders.Change);
        if (change is not (SagaTimeoutManagementHeaders.RescheduleChange or SagaTimeoutManagementHeaders.CancelChange))
        {
            throw new InvalidSagaTimeoutManagementRequestException($"The '{SagaTimeoutManagementHeaders.Change}' header must be '{SagaTimeoutManagementHeaders.RescheduleChange}' or '{SagaTimeoutManagementHeaders.CancelChange}', but is '{change}'.");
        }

        DateTimeOffset? dueTime = null;
        if (change == SagaTimeoutManagementHeaders.RescheduleChange)
        {
            var dueTimeValue = Required(headers, SagaTimeoutManagementHeaders.DueTime);
            try
            {
                dueTime = DateTimeOffsetHelper.ToDateTimeOffset(dueTimeValue);
            }
            catch (FormatException)
            {
                throw new InvalidSagaTimeoutManagementRequestException($"The '{SagaTimeoutManagementHeaders.DueTime}' header '{dueTimeValue}' isn't in the NServiceBus wire format for dates and times.");
            }
        }

        string? payloadContentType = null;
        if (!body.IsEmpty)
        {
            if (change != SagaTimeoutManagementHeaders.RescheduleChange)
            {
                throw new InvalidSagaTimeoutManagementRequestException("Only a reschedule request can carry a new timeout payload.");
            }

            payloadContentType = Required(headers, Headers.ContentType);
        }

        return new SagaTimeoutManagementRequest(
            RequiredGuid(headers, SagaTimeoutManagementHeaders.OperationId),
            Required(headers, SagaTimeoutManagementHeaders.SagaType),
            RequiredGuid(headers, SagaTimeoutManagementHeaders.SagaId),
            Required(headers, SagaTimeoutManagementHeaders.TimeoutId),
            int.TryParse(Required(headers, SagaTimeoutManagementHeaders.ExpectedVersion), NumberStyles.None, CultureInfo.InvariantCulture, out var version)
                ? version
                : throw new InvalidSagaTimeoutManagementRequestException($"The '{SagaTimeoutManagementHeaders.ExpectedVersion}' header isn't a valid version."),
            change,
            dueTime,
            payloadContentType);
    }

    static string Required(IReadOnlyDictionary<string, string> headers, string name) =>
        headers.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidSagaTimeoutManagementRequestException($"The saga timeout management request has no '{name}' header.");

    static Guid RequiredGuid(IReadOnlyDictionary<string, string> headers, string name) =>
        Guid.TryParse(Required(headers, name), out var value)
            ? value
            : throw new InvalidSagaTimeoutManagementRequestException($"The '{name}' header isn't a valid GUID.");
}

// Retrying a malformed request can't succeed, so it goes straight to the error queue
sealed class InvalidSagaTimeoutManagementRequestException(string message) : Exception(message);
