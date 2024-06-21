namespace NServiceBus;

using System.Diagnostics;

/// <summary>
/// Captures metric tags throughout the pipeline to add to applicable metrics
/// </summary>
class MetricTagsState
{
    public string[] MessageHandlerTypes { get; set; }
    public string MessageType { get; set; }
    public string EndpointDiscriminator { get; set; }
    public string QueueName { get; set; }

    public void AddMessageTypeIfExists(ref TagList tags)
    {
        if (!string.IsNullOrEmpty(MessageType))
        {
            tags.Add(new(MeterTags.MessageType, MessageType));
        }
    }

    public void AddMessageHandlerTypesIfExists(ref TagList tags)
    {
        if (MessageHandlerTypes == null)
        {
            return;
        }
        tags.Add(new(MeterTags.MessageHandlerTypes, string.Join(";", MessageHandlerTypes)));
    }

    public void AddEndpointDiscriminatorIfExists(ref TagList tags)
    {
        if (!string.IsNullOrEmpty(EndpointDiscriminator))
        {
            tags.Add(new(MeterTags.EndpointDiscriminator, EndpointDiscriminator));
        }
    }

    public void AddQueueNameIfExists(ref TagList tags)
    {
        if (!string.IsNullOrEmpty(QueueName))
        {
            tags.Add(new(MeterTags.QueueName, QueueName));
        }
    }
}