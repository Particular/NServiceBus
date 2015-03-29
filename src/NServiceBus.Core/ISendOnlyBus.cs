namespace NServiceBus
{
    using System;

    /// <summary>
    /// Provides the subset of bus operations that is applicable for a send only bus.
    /// </summary>
    public interface ISendOnlyBus: IDisposable
    {
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        void Publish(object message);

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        void Publish<T>();

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        void Publish<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        ICallback Send(object message);

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        /// <param name="context">The context for the send</param>
        ICallback Send(object message,SendContext context);

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        ICallback Send<T>(Action<T> messageConstructor);


        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <param name="context">The context for the send</param>
        ICallback Send<T>(Action<T> messageConstructor, SendContext context);

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        ICallback Send(string destination, string correlationId, object message);

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor);
    }


    /// <summary>
    /// Syntactic sugar for ISendOnlyBus
    /// </summary>
    public static class ISendOnlyBusExtensions
    {
        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">Object beeing extended</param>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        public static ICallback Send(this ISendOnlyBus bus, string destination, object message)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(message, "message");
       
            var options = new SendContext();
            
            options.SetDestination(destination);

            return bus.Send(message, options);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus"></param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static ICallback Send<T>(this ISendOnlyBus bus, string destination, Action<T> messageConstructor)
        {
            Guard.AgainstNullAndEmpty(destination, "destination");
            Guard.AgainstNull(messageConstructor, "messageConstructor");
           
            var context = new SendContext();

            context.SetDestination(destination);

            return bus.Send(messageConstructor, context);
        
        }

    
       
    }
}
