namespace NServiceBus;

using System.Diagnostics;

/// <summary>
/// Captures metric tags throughout the pipeline to add to applicable metrics
/// </summary>
class MetricTags
{
    public string[] MessageHandlerTypes { get; set; }

    public string MessageType { get; set; }

    public void AddMessageTypeIfExists(ref TagList tags) => tags.Add(new(MeterTags.MessageType, MessageType));

    public void AddMessageHandlerTypesIfExists(ref TagList tags)
    {
        if (MessageHandlerTypes == null)
        {
            return;
        }
        tags.Add(new(MeterTags.MessageHandlerTypes, string.Join(";", MessageHandlerTypes)));
    }
}