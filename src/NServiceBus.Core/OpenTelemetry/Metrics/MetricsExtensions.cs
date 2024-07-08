namespace NServiceBus;

using System;
using System.Collections.Generic;
using Extensibility;

static class MetricsExtensions
{
    public static bool TryGetTimeSent(this Dictionary<string, string> headers, out DateTimeOffset timeSent)
    {
        if (headers.TryGetValue(Headers.TimeSent, out var timeSentString))
        {
            timeSent = DateTimeOffsetHelper.ToDateTimeOffset(timeSentString);
            return true;
        }
        timeSent = DateTimeOffset.MinValue;
        return false;
    }

    public static bool TryGetDeliverAt(this Dictionary<string, string> headers, out DateTimeOffset deliverAt)
    {
        if (headers.TryGetValue(Headers.DeliverAt, out var deliverAtString))
        {
            deliverAt = DateTimeOffsetHelper.ToDateTimeOffset(deliverAtString);
            return true;
        }
        deliverAt = DateTimeOffset.MinValue;
        return false;
    }

    public static void SetPipelineStartedAt(this ContextBag contextBag, DateTimeOffset pipelineStartedAt) =>
        contextBag.Set("PipelineStartedAt", pipelineStartedAt);

    public static bool TryGetPipelineStartedAt(this ContextBag contextBag, out DateTimeOffset pipelineStartedAt) =>
        contextBag.TryGet("PipelineStartedAt", out pipelineStartedAt);
}