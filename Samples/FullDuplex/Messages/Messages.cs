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
        public List<string> Names { get; set; } 
    }

    [Serializable]
    public class DataResponseMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string Description { get; set; }

        // other data
    }
}
