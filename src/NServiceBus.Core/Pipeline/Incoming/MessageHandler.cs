namespace NServiceBus.Pipeline
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a message handler and its invocation.
    /// </summary>
    public class MessageHandler
    {
        /// <summary>
        /// Creates a new instance of the message handler with predefined invocation delegate and handler type.
        /// </summary>
        /// <param name="invocation">The invocation with context delegate.</param>
        /// <param name="handlerType">The handler type.</param>
        public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType)
        {
            HandlerType = handlerType;
            this.invocation = invocation;
        }

        /// <summary>
        /// The actual instance, can be a saga, a timeout or just a plain handler.
        /// </summary>
        public object Instance { get; set; }

        /// <summary>
        /// The handler type, can be a saga, a timeout or just a plain handler.
        /// </summary>
        public Type HandlerType { get; private set; }

        internal bool IsTimeoutHandler { get; set; }

        /// <summary>
        /// Invokes the message handler.
        /// </summary>
        /// <param name="message">the message to pass to the handler.</param>
        /// <param name="handlerContext">the context to pass to the handler.</param>
        public Task Invoke(object message, IMessageHandlerContext handlerContext)
        {
            return invocation(Instance, message, handlerContext);
        }

        Func<object, object, IMessageHandlerContext, Task> invocation;
    }
}