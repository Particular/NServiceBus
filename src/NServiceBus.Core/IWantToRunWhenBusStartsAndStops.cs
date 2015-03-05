namespace NServiceBus
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    public interface IWantToRunWhenBusStartsAndStops
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        void Start();

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop();
    }

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    public interface IRunWhenBusStartsAndStops
    {
        /// <summary>
        /// Method called at startup.
        /// </summary>
        void Start(RunContext context);

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop(RunContext context);
    }

#pragma warning disable 1591
    public class RunContext
    {
        readonly IBus bus;

        public RunContext(IBus bus)
        {
            this.bus = bus;
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        public void Publish(object message)
        {
            bus.Publish(message);
        }

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        public void Publish<T>()
        {
            bus.Publish<T>();
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public void Publish<T>(Action<T> messageConstructor)
        {
            bus.Publish(messageConstructor);
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="message">The message to send.</param>
        public ICallback Send(object message)
        {
            return bus.Send(message);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return bus.Send(messageConstructor);
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        public ICallback Send(string destination, object message)
        {
            return bus.Send(destination, message);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return bus.Send(destination, messageConstructor);
        }

        /// <summary>
        /// Sends the message to the destination as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        public ICallback Send(string destination, string correlationId, object message)
        {
            return bus.Send(destination, correlationId, message);
        }

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the destination identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return bus.Send(destination, correlationId, messageConstructor);
        }

        /// <summary>
        /// Gets the list of key/value pairs that will be in the header of
        /// messages being sent by the same thread.
        /// 
        /// This value will be cleared when a thread receives a message.
        /// </summary>
        public IDictionary<string, string> OutgoingHeaders
        {
            get { return this.bus.OutgoingHeaders; }
        }
    }
#pragma warning restore 1591
}
