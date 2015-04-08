namespace NServiceBus
{
    using System;

    /// <summary>
    /// Defines a bus to be used with NServiceBus.
    /// </summary>
    public interface IBus : ISendOnlyBus
    {
        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Subscribe(Type messageType);

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        void Subscribe<T>();

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="messageType">The type of message to subscribe to.</param>
        void Unsubscribe(Type messageType);

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        void Unsubscribe<T>();

        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="message">The message to send.</param>
        void Reply(object message);

        /// <summary>
        /// Instantiates a message of type T and performs a regular <see cref="Reply(object)"/>.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Reply<T>(Action<T> messageConstructor);

        /// <summary>
        /// Returns a completion message with the specified error code to the sender
        /// of the message being handled. The type T can only be an enum or an integer.
        /// </summary>
        void Return<T>(T errorEnum);

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        void HandleCurrentMessageLater();

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        void ForwardCurrentMessageTo(string destination);

        /// <summary>
        /// Tells the bus to stop dispatching the current message to additional
        /// handlers.
        /// </summary>
        void DoNotContinueDispatchingCurrentMessageToHandlers();

        /// <summary>
        /// Gets the message context containing the Id, return address, and headers
        /// of the message currently being handled on this thread.
        /// </summary>
        IMessageContext CurrentMessageContext { get; }
    }

    /// <summary>
    /// Syntactic sugar for IBus
    /// </summary>
    public static class IBusExtensions
    {
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static ICallback SendLocal(this IBus bus, object message)
        {
            Guard.AgainstNull(message, "message");

            var context = new SendOptions();

            context.SetLocalEndpointAsDestination();

            return bus.Send(message, context);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static ICallback SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull(messageConstructor, "messageConstructor");
            var context = new SendOptions();

            context.SetLocalEndpointAsDestination();

            return bus.Send(messageConstructor, context);
        }

        /// <summary>
        /// Defers the processing of the message for the given delay. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        public static ICallback Defer(this IBus bus, TimeSpan delay, object message)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNegativeAndZero(delay,"delay");
            
            var context = new SendOptions();

            context.DelayDeliveryWith(delay);
            context.SetLocalEndpointAsDestination();

            return bus.Send(message, context);
        }

        /// <summary>
        /// Defers the processing of the message until the specified time. This feature is using the timeout manager so make sure that you enable timeouts
        /// </summary>
        public static ICallback Defer(this IBus bus, DateTime processAt, object message)
        {
            Guard.AgainstNull(message, "message");
            Guard.AgainstNull(processAt, "processAt");
        
            var context = new SendOptions();

            context.DeliverAt(processAt);
            context.SetLocalEndpointAsDestination();

            return bus.Send(message, context);
        }
    }
}
