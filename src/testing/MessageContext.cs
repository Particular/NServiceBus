using System;
using System.Collections.Generic;

namespace NServiceBus.Testing
{
    class MessageContext : IMessageContext
    {
        public string Id { get; set; }
        public string ReturnAddress
        {
            get { return ReplyToAddress.ToString();  }
            set { ReplyToAddress = Address.Parse(value);  }
        }

        public Address ReplyToAddress { get; set; }

        public DateTime TimeSent { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }
}
