using NServiceBus;

namespace Messages
{
    public class MessageWithByteArray : IMessage
    {
        public byte[] Buffer { get; set; }
    }
}
