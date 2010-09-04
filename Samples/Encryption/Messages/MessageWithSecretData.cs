using NServiceBus;

namespace Messages
{
    public class MessageWithSecretData : IMessage
    {
        public WireEncryptedString Secret { get; set; }
    }
}
