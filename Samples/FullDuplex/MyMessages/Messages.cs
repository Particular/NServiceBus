using NServiceBus;
using System;

namespace MyMessages
{
    public class RequestDataMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
        public string Question { get; set; }
    }

    public class DataResponseMessage : IMessage
    {
        public Guid DataId { get; set; }
        public string String { get; set; }
        public string Answer { get; set; }
    }
}
