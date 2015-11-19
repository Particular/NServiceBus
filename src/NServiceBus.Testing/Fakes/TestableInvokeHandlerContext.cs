namespace NServiceBus.Testing.Fakes
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using NServiceBus.Pipeline.Contexts;
    using NServiceBus.Unicast.Behaviors;
    using NServiceBus.Unicast.Messages;

    public class TestableInvokeHandlerContext : TestableIncomingContext, InvokeHandlerContext
    {
        public bool HandleCurrentMessageLaterCalled { get; private set; }
        public Task HandleCurrentMessageLaterAsync()
        {
            HandleCurrentMessageLaterCalled = true;
            return Task.CompletedTask;
        }

        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            HandlerInvocationAborted = true;
        }

        public MessageHandler MessageHandler { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public object MessageBeingHandled { get; set; }
        public bool HandlerInvocationAborted { get; set; }
        public MessageMetadata MessageMetadata { get; set; }
    }
}