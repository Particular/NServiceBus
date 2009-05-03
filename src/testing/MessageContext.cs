using System;
using System.Collections.Generic;

namespace NServiceBus.Testing
{
    class MessageContext : IMessageContext
    {
        public string Id { get; set; }
        public string ReturnAddress { get; set; }
        public IDictionary<string, string> Headers { get; set; }
    }
}
