namespace NServiceBus.Testing
{
    using System;
    using System.Collections.Generic;

    class MessageContext : IMessageContext
    {
        public string Id { get; set; }
        public string ReturnAddress
        {
            get { return ReplyToAddress;  }
            set { ReplyToAddress = value;  }
        }

        public string ReplyToAddress { get; set; }

        public DateTime TimeSent { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }
}
