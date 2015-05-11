namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for IRunContext
    /// </summary>
    public static class IRunContextExtensions
    {
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="context">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static void SendLocal(this IRunContext context, object message)
        {
            Guard.AgainstNull(context, "context");
            Guard.AgainstNull(message, "message");

            context.SendLocal(message, new SendLocalOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="context">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void SendLocal<T>(this IRunContext context, Action<T> messageConstructor)
        {
            Guard.AgainstNull(context, "context");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            context.SendLocal(messageConstructor, new SendLocalOptions());
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="context">The object beeing extended</param>
        /// <param name="message">The message to publish</param>
        public static void Publish(this IRunContext context, object message)
        {
            context.Publish(message, new PublishOptions());
        }

       
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="context">Object beeing extended</param>
        /// <typeparam name="T">The message type</typeparam>
        public static void Publish<T>(this IRunContext context)
        {
            context.Publish<T>(_=>{},new PublishOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="context">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void Publish<T>(this IRunContext context, Action<T> messageConstructor)
        {
            context.Publish(messageConstructor,new PublishOptions());
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="context">Object beeing extended</param>
        /// <param name="message">The message to send.</param>
        public static void Send(this IRunContext context, object message)
        {
            Guard.AgainstNull(context, "bus");
            Guard.AgainstNull(message, "message");

            context.Send(message, new SendOptions());
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="context">Object beeing extended</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        public static void Send<T>(this IRunContext context, Action<T> messageConstructor)
        {
            Guard.AgainstNull(context, "bus");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            context.Send(messageConstructor, new SendOptions());
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="context">Object beeing extended</param>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        public static void Send(this IRunContext context, string destination, object message)
        {
            Guard.AgainstNull(context, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(message, "message");

            context.Send(message, new SendOptions(destination));
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="context"></param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static void Send<T>(this IRunContext context, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNull(context, "bus");
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(messageConstructor, "messageConstructor");

            context.Send(messageConstructor, new SendOptions(destination));
        }
    }
}