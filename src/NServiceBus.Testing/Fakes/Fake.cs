namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using NServiceBus.ObjectBuilder;
    using NServiceBus.OutgoingPipeline;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Routing;
    using NServiceBus.Transports;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;

    public class Fake
    {
        public static TestableTransportReceiveContext CreateTransportReceiveContext(string messageId = null, Dictionary<string, string> headers = null, byte[] body = null, PipelineInfo pipelineInfo = null, IBuilder builder = null, BehaviorContext parent = null)
        {


            Stream bodyStream = new MemoryStream(body ?? new byte[0]);

            var incomingMessage = new IncomingMessage(
                messageId ?? Guid.NewGuid().ToString(), 
                headers ?? new Dictionary<string, string>(), 
                bodyStream);

            var parentContext = parent ?? new RootContext(builder ?? new FakeBuilder());
            
            return new TestableTransportReceiveContext(
                incomingMessage, 
                pipelineInfo ?? new PipelineInfo("pipelineName", "piplineTransportAddress"),
                parentContext);
        }

        public static TestableOutgoingLogicalMessageContext CreateOutgoingLogicalMessageContext(IReadOnlyCollection<RoutingStrategy> routingStrategies = null, string messageId = null, Dictionary<string, string> headers = null, OutgoingLogicalMessage logicalMessage = null, IBuilder builder = null, BehaviorContext parent = null)
        {
            if (builder != null && parent != null)
            {
                throw new ArgumentException(
                    "You can't provide a custom builder when providing a parent context at the same time.",
                    nameof(builder));
            }

            var parentContext = parent ?? new RootContext(builder ?? new FakeBuilder());

            return new TestableOutgoingLogicalMessageContext(
                messageId ?? Guid.NewGuid().ToString(), 
                headers ?? new Dictionary<string, string>(), 
                logicalMessage ?? new OutgoingLogicalMessage(new object()), 
                routingStrategies ?? new List<RoutingStrategy>(),
                parentContext);
        }

        public static TestableInvokeHandlerContext CreateInvokeHandlerContext(
            MessageHandler handler = null, 
            string messageId = null, 
            string replyToAddress = null, 
            Dictionary<string, string> headers = null, 
            MessageMetadata messageMetadata = null, 
            object incomingMessage = null, 
            PipelineInfo pipelineInfo = null, 
            IBuilder builder = null)
        {
            return new TestableInvokeHandlerContext(
                handler ?? new MessageHandler((o, o1, arg3) => Task.CompletedTask, typeof(object)),
                messageId ?? Guid.NewGuid().ToString(),
                replyToAddress ?? string.Empty,
                headers ?? new Dictionary<string, string>(),
                messageMetadata ?? new MessageMetadata(typeof(object)),
                incomingMessage ?? new object(),
                pipelineInfo ?? new PipelineInfo("pipelineName", "transportAddress"),
                new RootContext(builder ?? new FakeBuilder()));
        }
    }
}