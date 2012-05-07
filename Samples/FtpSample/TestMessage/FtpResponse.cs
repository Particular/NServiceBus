using System;
using NServiceBus;

namespace TestMessage
{
    [Serializable]
    public class FtpResponse : IMessage
    {
        public int Id { get; set; }
        public Guid OtherData { get; set; }
        public bool IsThisCool { get; set; }
        public String Description { get; set; }
        public int ResponseId { get; set; }
    }
}
