namespace NServiceBus.Gateway.Persistence.Raven
{
    using System;
    using System.Collections.Generic;

    public class GatewayMessage
    {
        public IDictionary<string, string> Headers { get; set; }
       
        public DateTime TimeReceived { get; set; }
        
        public string Id { get; set; }

        public byte[] OriginalMessage { get; set; }

        public bool Acknowledged { get; set; }
    }
}