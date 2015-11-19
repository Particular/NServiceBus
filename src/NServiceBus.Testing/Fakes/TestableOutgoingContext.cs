namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.OutgoingPipeline;

    public class TestableOutgoingContext : TestableBusContext, OutgoingContext
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();
    }
}