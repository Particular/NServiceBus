using NServiceBus;
using System;
using System.Collections.Generic;

namespace Messages
{
    [Serializable]
    public class RequestDataMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
        public WireEncryptedString SecretQuestion { get; set; }
    }

    [Serializable]
    public class DataResponseMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
        public WireEncryptedString SecretAnswer { get; set; }
    }
}
