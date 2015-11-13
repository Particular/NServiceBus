namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Transport;

    public class Fake
    {
        public static TestableTransportReceiveContext CreateTransportReceiveContext(string messageId = null, Dictionary<string, string> headers = null, byte[] body = null, PipelineInfo pipelineInfo = null, IBuilder builder = null)
        {
            Stream bodyStream = new MemoryStream(body ?? new byte[0]);

            var incomingMessage = new IncomingMessage(
                messageId ?? Guid.NewGuid().ToString(), 
                headers ?? new Dictionary<string, string>(), 
                bodyStream);

            BehaviorContext parent = new RootContext(builder ?? new FakeBuilder());
            
            return new TestableTransportReceiveContext(
                incomingMessage, 
                pipelineInfo ?? new PipelineInfo("pipelineName", "piplineTransportAddress"), 
                parent);
        }
    }
}