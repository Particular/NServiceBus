using NServiceBus;

namespace Messages
{
    public class MessageWithSecretData : IMessage
    {
        public WireEncryptedString Secret { get; set; }
        public MySecretSubProperty SubProperty{ get; set; }
        
    }

    public class MySecretSubProperty
    {
        public WireEncryptedString Secret { get; set; }
    }
}
