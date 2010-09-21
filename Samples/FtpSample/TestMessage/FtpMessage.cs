using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NServiceBus;

namespace TestMessage
{
    [Serializable]
    public class FtpMessage : IMessage
    {
        public int ID { get; set;  }

        public String Name { get; set; }
    }
}
