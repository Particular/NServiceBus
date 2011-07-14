using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace TestMessage
{
    [Serializable]
    public class FtpReply : IMessage
    {
        public int ID { get; set; }
        public Guid OtherData { get; set; }
        public bool IsThisCool { get; set; }
        public String Description { get; set; }
    }
}
