namespace NServiceBus.Testing.Fakes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
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

        public BusOperations BusOperations { get; } = new BusOperations();

        public bool HandleCurrentMessageLaterCalled { get; private set; }

        public override Task HandleCurrentMessageLaterAsync()
        {
            HandleCurrentMessageLaterCalled = true;
            return Task.CompletedTask;
        }

        public override void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            // This already sets a public property which can be used for testing
            base.DoNotContinueDispatchingCurrentMessageToHandlers();
        }

        public override Task SendAsync(object message, SendOptions options)
        {
            return BusOperations.SendAsync(message, options);
        }

        public override Task SendAsync<T>(Action<T> messageConstructor, SendOptions options)
        {
            return BusOperations.SendAsync(messageConstructor, options);
        }

        public override Task PublishAsync(object message, PublishOptions options)
        {
            return BusOperations.PublishAsync(message, options);
        }

        public override Task PublishAsync<T>(Action<T> messageConstructor, PublishOptions publishOptions)
        {
            return BusOperations.PublishAsync(messageConstructor, publishOptions);
        }

        public override Task SubscribeAsync(Type eventType, SubscribeOptions options)
        {
            return BusOperations.SubscribeAsync(eventType, options);
        }

        public override Task UnsubscribeAsync(Type eventType, UnsubscribeOptions options)
        {
            return BusOperations.UnsubscribeAsync(eventType, options);
        }

        public override Task ReplyAsync(object message, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public override Task ReplyAsync<T>(Action<T> messageConstructor, ReplyOptions options)
        {
            throw new NotImplementedException();
        }

        public override Task ForwardCurrentMessageToAsync(string destination)
        {
            throw new NotImplementedException();
        }

        public static implicit operator BusOperations(TestableInvokeHandlerContext ctx)
        {
            return ctx.BusOperations;
        }
    }
}