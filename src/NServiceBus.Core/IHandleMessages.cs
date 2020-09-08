namespace NServiceBus
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines a message handler.
    /// </summary>
    /// <typeparam name="T">The type of message to be handled.</typeparam>
    public interface IHandleMessages<T>
    {
        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="context">The context of the currently handled message.</param>
        /// <remarks>
        /// This method will be called when a message arrives on at the endpoint and should contain
        /// the custom logic to execute when the message is received.
        /// </remarks>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Handle(T message, IMessageHandlerContext context)
        {
            return Handle(message, context, CancellationToken.None);
        }

        /// <summary>
        /// Handles a message.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="context">The context of the currently handled message.</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <remarks>
        /// This method will be called when a message arrives on at the endpoint and should contain
        /// the custom logic to execute when the message is received.
        /// </remarks>
        /// <exception cref="System.Exception">This exception will be thrown if <code>null</code> is returned. Return a Task or mark the method as <code>async</code>.</exception>
        Task Handle(T message, IMessageHandlerContext context, CancellationToken cancellationToken)
        {
            return Handle(message, context);
        }
    }
}