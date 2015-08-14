namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for <see cref="IBus"/>.
    /// </summary>
    public static class IBusExtensions
    {
        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static void Reply(this IBus bus, object message)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("message", message);

            bus.Reply(message, new ReplyOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and performs a regular Reply.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static void Reply<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("messageConstructor", messageConstructor);

            bus.Reply(messageConstructor, new ReplyOptions());
        }
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        public static void SendLocal(this IBus bus, object message)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("message", message);

            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            bus.Send(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("messageConstructor", messageConstructor);

            var options = new SendOptions();

            options.RouteToLocalEndpointInstance();

            bus.Send(messageConstructor, options);
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static void Subscribe(this IBus bus, Type messageType)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("messageType", messageType);

            bus.Subscribe(messageType, new SubscribeOptions());
        }

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        public static void Subscribe<T>(this IBus bus)
        {
            Guard.AgainstNull("bus", bus);

            bus.Subscribe(typeof(T), new SubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        public static void Unsubscribe(this IBus bus, Type messageType)
        {
            Guard.AgainstNull("bus", bus);
            Guard.AgainstNull("messageType", messageType);

            bus.Unsubscribe(messageType, new UnsubscribeOptions());
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        public static void Unsubscribe<T>(this IBus bus)
        {
            Guard.AgainstNull("bus", bus);

            bus.Unsubscribe(typeof(T), new UnsubscribeOptions());
        }
    }
}