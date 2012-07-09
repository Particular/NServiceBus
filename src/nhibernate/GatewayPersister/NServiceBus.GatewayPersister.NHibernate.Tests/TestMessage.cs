namespace NServiceBus.GatewayPersister.NHibernate.Tests
{
    using System;
    using System.Collections.Generic;

    public class TestMessage
    {
        public IDictionary<string, string> Headers { get; set; }

        public DateTime TimeReceived { get; set; }

        public string ClientId { get; set; }

        public byte[] OriginalMessage { get; set; }

        public bool Acknowledged { get; set; }
    }
}