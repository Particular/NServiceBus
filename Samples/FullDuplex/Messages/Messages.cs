using NServiceBus;
using System;

namespace Messages
{
    public class RequestDataMessage : IMessage
    {
        private RequestDataMessage() // now showing that we can handle non-default constructors
        {
        }

        public Guid DataId { get; set; }
        public string String { get; set; }
        public WireEncryptedString SecretQuestion { get; set; }
    }

    public class DataResponseMessage : IMessage
    {
        private DataResponseMessage() // now showing that we can handle non-default constructors
        {
        }

        public Guid DataId { get; set; }
        public string String { get; set; }
        public WireEncryptedString SecretAnswer { get; set; }
    }
}
