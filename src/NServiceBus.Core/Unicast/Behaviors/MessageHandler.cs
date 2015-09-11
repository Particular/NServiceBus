namespace NServiceBus.Unicast.Behaviors
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a message handler and its invocation.
    /// </summary>
    public partial class MessageHandler
    {
        Func<object, object, Task> invocation;

        /// <summary>
        /// Creates a new instance of the message handler with predefined invocation delegate and handler type.
        /// </summary>
        /// <param name="invocation">The invocation with context delegate.</param>
        /// <param name="handlerType">The handler type.</param>
        internal MessageHandler(Func<object, object, Task> invocation, Type handlerType)
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

        /// <summary>
        /// Invokes the message handler.
        /// </summary>
        /// <param name="message">the message to pass to the handler.</param>
        public Task Invoke(object message)
        {
            return invocation(Instance, message);
        }
    }
}