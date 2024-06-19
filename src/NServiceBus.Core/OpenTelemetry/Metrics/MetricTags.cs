namespace NServiceBus;

using System.Collections.Generic;
using System.Diagnostics;

/// <summary>
/// Captures metric tags throughout the pipeline to add to applicable metrics
/// </summary>
class MetricTags
{
    public string[] MessageHandlerTypes { get; set; }

    public string MessageType { get; set; }

    public void ApplyToTags(TagList tags)
    {
        if (!string.IsNullOrEmpty(MessageType))
        {
            tags.Add(new KeyValuePair<string, object>(MeterTags.MessageType, MessageType));
        }
        if (MessageHandlerTypes.Length != 0)
        {
            tags.Add(new KeyValuePair<string, object>(MeterTags.MessageHandlerTypes, string.Join(";", MessageHandlerTypes)));
        }
    }
}