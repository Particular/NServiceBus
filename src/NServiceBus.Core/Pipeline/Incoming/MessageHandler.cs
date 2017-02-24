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
        public MessageHandler(Func<object, object, IMessageHandlerContext, Task> invocation, Type handlerType) : this(new[] { invocation }, handlerType)
        {
        }

        /// <summary>
        /// Creates a new instance of the message handler with predefined invocation delegate and handler type.
        /// </summary>
        /// <param name="invocations">The invocation with context delegate.</param>
        /// <param name="handlerType">The handler type.</param>
        public MessageHandler(Func<object, object, IMessageHandlerContext, Task>[] invocations, Type handlerType)
        {
            HandlerType = handlerType;
            this.invocations = invocations;
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
        /// Performs the invocations.
        /// </summary>
        /// <param name="message">the message to pass to the handler.</param>
        /// <param name="handlerContext">the context to pass to the handler.</param>
        public async Task Invoke(object message, IMessageHandlerContext handlerContext)
        {
            foreach (var invocation in invocations)
            {
                await invocation(Instance, message, handlerContext).ConfigureAwait(false);
            }
        }

        Func<object, object, IMessageHandlerContext, Task>[] invocations;
    }
}