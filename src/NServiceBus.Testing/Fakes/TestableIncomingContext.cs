namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Transport;

    public class TestableIncomingContext : TestableBusContext, IncomingContext
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string ReplyToAddress { get; set; } = "ReplyToAddress";
        public PipelineInfo PipelineInfo { get; set; } = new PipelineInfo("PipelineName", "PipelineTransportAddress");
        public IReadOnlyDictionary<string, string> MessageHeaders { get; set; } = new Dictionary<string, string>();

        public Task ReplyAsync(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public Task ForwardCurrentMessageToAsync(string destination)
        {
            throw new NotImplementedException();
        }
    }
}