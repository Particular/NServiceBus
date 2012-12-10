using System;
using NServiceBus;

namespace TestMessage
{
    [Serializable]
    public class FtpMessage : ICommand
    {
        public int Id { get; set;  }
        public String Name { get; set; }
    }
}
