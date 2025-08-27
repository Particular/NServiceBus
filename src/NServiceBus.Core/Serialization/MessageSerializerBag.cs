namespace NServiceBus;

using NServiceBus.Serialization;

class MessageSerializerBag(IMessageSerializer messageSerializer, bool supportsZeroLengthMessages)
{
    public IMessageSerializer MessageSerializer { get; set; } = messageSerializer;
    public bool SupportsZeroLengthMessages { get; set; } = supportsZeroLengthMessages;
}