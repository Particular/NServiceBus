namespace NServiceBus
{
    using System;

    /// <summary>
    /// Syntactic sugar for <see cref="IBusInterface"/>.
    /// </summary>
    public static class ISendOnlyBusExtensions
    {
        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Publish(object message)")]
        public static void Publish(this IBusInterface bus, object message)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <typeparam name="T">The message type.</typeparam>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Publish()")]
        public static void Publish<T>(this IBusInterface bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "PublishAsync<T>(Action<T> messageConstructor)")]
        public static void Publish<T>(this IBusInterface bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Send(object message)")]
        public static void Send(this IBusInterface bus, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of <typeparamref name="T"/> and sends it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        /// <remarks>
        /// The message will be sent to the destination configured for <typeparamref name="T"/>.
        /// </remarks>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Send<T>(Action<T> messageConstructor)")]
        public static void Send<T>(this IBusInterface bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Sends the message.
        /// </summary>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="destination">The address of the destination to which the message will be sent.</param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Send(string destination, object message)")]
        public static void Send(this IBusInterface bus, string destination, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given destination.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="IBusInterface"/> to use for the action.</param>
        /// <param name="destination">The destination to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "Send<T>(string destination, Action<T> messageConstructor)")]
        public static void Send<T>(this IBusInterface bus, string destination, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }
}