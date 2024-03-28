namespace NServiceBus;
using System;

static class Extensions
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
}