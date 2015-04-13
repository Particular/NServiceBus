namespace NServiceBus
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// Implementers will be invoked when the endpoint starts up.
    /// Dependency injection is provided for these types.
    /// </summary>
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
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
        void Start(IRunContext context);

        /// <summary>
        /// Method called on shutdown.
        /// </summary>
        void Stop(IRunContext context);
    }

#pragma warning disable 1591
    public interface IRunContext
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
        /// Instantiates a message of type T and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        /// <remarks>
        /// The message will be sent to the destination configured for T
        /// </remarks>
        ICallback Send<T>(Action<T> messageConstructor);

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="destination">
        /// The address of the destination to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        ICallback Send(string destination, object message);

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        ICallback Send<T>(string destination, Action<T> messageConstructor);

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

    class RunContext : IRunContext
    {
        readonly IBus bus;

        public RunContext(IBus bus)
        {
            this.bus = bus;
        }

        public void Publish(object message)
        {
            bus.Publish(message);
        }

        public void Publish<T>()
        {
            bus.Publish<T>();
        }

        public void Publish<T>(Action<T> messageConstructor)
        {
            bus.Publish(messageConstructor);
        }

        public ICallback Send(object message)
        {
            return bus.Send(message);
        }

        public ICallback Send<T>(Action<T> messageConstructor)
        {
            return bus.Send(messageConstructor);
        }

        public ICallback Send(string destination, object message)
        {
            return bus.Send(destination, message);
        }

        public ICallback Send<T>(string destination, Action<T> messageConstructor)
        {
            return bus.Send(destination, messageConstructor);
        }

        public ICallback Send(string destination, string correlationId, object message)
        {
            return bus.Send(destination, correlationId, message);
        }
        public ICallback Send<T>(string destination, string correlationId, Action<T> messageConstructor)
        {
            return bus.Send(destination, correlationId, messageConstructor);
        }
    }
#pragma warning restore 1591
}
