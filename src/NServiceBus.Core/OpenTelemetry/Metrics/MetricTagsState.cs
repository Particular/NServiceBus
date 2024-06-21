namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;

static class MetricTagsExtensions
{
    public const string AvailableMetricsTags = "NServiceBus.OpenTelemetry.AvailableMetricsState";
    public static void Apply(this Dictionary<string, object> availableTags, ref TagList taglist, params string[] tagKeys)
    {
        foreach (var tagKey in tagKeys)
        {
            if (availableTags.TryGetValue(tagKey, out var value))
            {
                taglist.Add(new(tagKey, value));
            }
        }
    }
}