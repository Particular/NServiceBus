namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    public class TestableTransportReceiveContext : TestableBehaviorContext, TransportReceiveContext
    {
        public IncomingMessage Message { get; set; } = new IncomingMessage(Guid.NewGuid().ToString(), new Dictionary<string, string>(), new MemoryStream());
        public PipelineInfo PipelineInfo { get; set; } = new PipelineInfo("PipelineName", "PipelineTransportAddress");
    }
}