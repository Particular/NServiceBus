using NServiceBus;
using System;

namespace Messages
{
    [Serializable]
    public class RequestDataMessage : IMessage
    {
        public Guid DataId;
    }

    [Serializable]
    public class DataResponseMessage : IMessage
    {
        public Guid DataId;
        public string Description;

        // other data
    }
}
