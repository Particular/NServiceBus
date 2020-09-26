namespace NServiceBus.Testing
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Persistence;
    using Pipeline;
    using Unicast.Messages;

    /// <summary>
    /// A testable implementation of <see cref="IInvokeHandlerContext" />.
    /// </summary>
    public partial class TestableInvokeHandlerContext : TestableIncomingContext, IInvokeHandlerContext
    {
        /// <summary>
        /// Creates a new instance of <see cref="TestableInvokeHandlerContext" />.
        /// </summary>
        public TestableInvokeHandlerContext(IMessageCreator messageCreator = null) : base(messageCreator)
        {
        }

        /// <summary>
        /// Indicates if <see cref="IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> has been called.
        /// </summary>
        public bool DoNotContinueDispatchingCurrentMessageToHandlersWasCalled { get; set; }

        /// <summary>
        /// Tells the endpoint to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        public void DoNotContinueDispatchingCurrentMessageToHandlers()
        {
            DoNotContinueDispatchingCurrentMessageToHandlersWasCalled = true;
        }

        /// <summary>
        /// Gets the synchronized storage session for processing the current message. NServiceBus makes sure the changes made
        /// via this session will be persisted before the message receive is acknowledged.
        /// </summary>
        public SynchronizedStorageSession SynchronizedStorageSession { get; set; }

        /// <summary>
        /// The current <see cref="T:NServiceBus.IHandleMessages`1" /> being executed.
        /// </summary>
        public MessageHandler MessageHandler { get; set; } = new MessageHandler((instance, message, context) => Task.FromResult(0), typeof(object));

        /// <summary>
        /// Message headers.
        /// </summary>
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// The message instance being handled.
        /// </summary>
        public object MessageBeingHandled { get; set; } = new object();

        /// <summary>
        /// <code>true</code> if
        /// <see cref="M:NServiceBus.IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers" /> has been called.
        /// </summary>
        public bool HandlerInvocationAborted => DoNotContinueDispatchingCurrentMessageToHandlersWasCalled;

        /// <summary>
        /// Metadata for the incoming message.
        /// </summary>
        public MessageMetadata MessageMetadata { get; set; } = new MessageMetadata(typeof(object));
    }
}