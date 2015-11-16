namespace NServiceBus.Testing.Fakes
{
    using System.Collections.Generic;
    using NServiceBus.Pipeline;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;
    using NServiceBus.Unicast.Transport;

    public class TestableInvokeHandlerContext : InvokeHandlerContext
    {
        public TestableInvokeHandlerContext(MessageHandler handler, string messageId, string replyToAddress, Dictionary<string, string> headers, MessageMetadata messageMetadata, object messageBeingHandled, PipelineInfo pipelineInfo, BehaviorContext parentContext) 
            : base(handler, messageId, replyToAddress, headers, messageMetadata, messageBeingHandled, pipelineInfo, parentContext)
        {
        }
    }
}