#nullable enable

namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;

static class ActivityDecorator
{
    public static void PromoteHeadersToTags(Activity activity, Dictionary<string, string> headers)
    {
        foreach (var header in headers)
        {
            if (HeaderMapping.TryGetValue(header.Key, out var tagName))
            {
                activity.AddTag(tagName, TagValue(header.Key, header.Value));
            }
        }
    }

    // The enclosed message types header is a list, and the OpenTelemetry naming rules ask for an array when an
    // attribute holds several values. Keep only the array branch in v11, see obsoletes-v10.cs.
    static object TagValue(string headerKey, string headerValue) =>
        V11BehaviorSwitch.UseV11Behavior && headerKey == Headers.EnclosedMessageTypes
            ? headerValue.Split(EnclosedMessageTypesSeparator)
            : headerValue;

    const char EnclosedMessageTypesSeparator = ';';

    internal static readonly Dictionary<string, string> HeaderMapping = new()
    {
        { Headers.SagaId, ActivityTags.SagaId },
        { Headers.MessageId, ActivityTags.MessageId },
        { Headers.CorrelationId, ActivityTags.CorrelationId },
        { Headers.ReplyToAddress, ActivityTags.ReplyToAddress },
        { Headers.NServiceBusVersion, ActivityTags.NServiceBusVersion },
        { Headers.ControlMessageHeader, ActivityTags.ControlMessageHeader },
        { Headers.SagaType, ActivityTags.SagaType },
        { Headers.OriginatingSagaId, ActivityTags.OriginatingSagaId },
        { Headers.OriginatingSagaType, ActivityTags.OriginatingSagaType },
        { Headers.DelayedRetries, ActivityTags.DelayedRetries },
        { Headers.DelayedRetriesTimestamp, ActivityTags.DelayedRetriesTimestamp },
        { Headers.DeliverAt, ActivityTags.DeliverAt },
        { Headers.RelatedTo, ActivityTags.RelatedTo },
        { Headers.EnclosedMessageTypes, ActivityTags.EnclosedMessageTypes },
        { Headers.ContentType, ActivityTags.ContentType },
        { Headers.IsSagaTimeoutMessage, ActivityTags.IsSagaTimeoutMessage },
        { Headers.IsDeferredMessage, ActivityTags.IsDeferredMessage },
        { Headers.OriginatingEndpoint, ActivityTags.OriginatingEndpoint },
        { Headers.OriginatingMachine, ActivityTags.OriginatingMachine },
        { Headers.OriginatingHostId, ActivityTags.OriginatingHostId },
        { Headers.ProcessingEndpoint, ActivityTags.ProcessingEndpoint },
        { Headers.ProcessingMachine, ActivityTags.ProcessingMachine },
        { Headers.HostDisplayName, ActivityTags.HostDisplayName },
        { Headers.HostId, ActivityTags.HostId },
        { Headers.HasLicenseExpired, ActivityTags.HasLicenseExpired },
        { Headers.OriginatingAddress, ActivityTags.OriginatingAddress },
        { Headers.ConversationId, ActivityTags.ConversationId },
        { Headers.PreviousConversationId, ActivityTags.PreviousConversationId },
        { Headers.MessageIntent, ActivityTags.MessageIntent },
        { Headers.TimeToBeReceived, ActivityTags.TimeToBeReceived },
        { Headers.DataBusConfigContentType, ActivityTags.DataBusContentType },
        { Headers.NonDurableMessage, ActivityTags.NonDurableMessage }
    };
}