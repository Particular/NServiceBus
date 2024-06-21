namespace NServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;

static class MetricsExtensions
{
    public static bool TryGetTimeSent(this ReceivePipelineCompleted completed, out DateTimeOffset timeSent)
    {
        var headers = completed.ProcessedMessage.Headers;
        if (headers.TryGetValue(Headers.TimeSent, out var timeSentString))
        {
            timeSent = DateTimeOffsetHelper.ToDateTimeOffset(timeSentString);
            return true;
        }
        timeSent = DateTimeOffset.MinValue;
        return false;
    }

    public static bool TryGetDeliverAt(this ReceivePipelineCompleted completed, out DateTimeOffset deliverAt)
    {
        var headers = completed.ProcessedMessage.Headers;
        if (headers.TryGetValue(Headers.DeliverAt, out var deliverAtString))
        {
            deliverAt = DateTimeOffsetHelper.ToDateTimeOffset(deliverAtString);
            return true;
        }
        deliverAt = DateTimeOffset.MinValue;
        return false;
    }

    public static bool TryGetMessageType(this ReceivePipelineCompleted completed, out string processedMessageType)
        => completed.ProcessedMessage.Headers.TryGetMessageType(out processedMessageType);

    static bool TryGetMessageType(this Dictionary<string, string> headers, out string processedMessageType)
    {
        if (headers.TryGetValue(Headers.EnclosedMessageTypes, out var enclosedMessageType))
        {
            string messageTypeHeader = !string.IsNullOrEmpty(enclosedMessageType) ? enclosedMessageType.Split(';').FirstOrDefault() : default;
            string messageTypeName = !string.IsNullOrEmpty(messageTypeHeader) ? messageTypeHeader.Split(',').FirstOrDefault() : default;
            processedMessageType = messageTypeName;
            return true;
        }
        processedMessageType = null;
        return false;
    }

}