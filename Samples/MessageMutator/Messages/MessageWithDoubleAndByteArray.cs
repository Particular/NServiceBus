using NServiceBus;

namespace Messages
{
    public class MessageWithDoubleAndByteArray : IMessage
    {
        public double MyDouble { get; set; }
        public byte[] Buffer { get; set; }
    }
}
