namespace NServiceBus.Msmq
{
    using System;

    /// <summary>
    /// Extensions to IBus to allow stronger typed sending of by <see cref="MsmqAddress"/>.
    /// </summary>
    public static class MsmqBusExtensions
    {
        /// <summary>
        /// Sends the provided message.
        /// </summary>
        /// <param name="bus">The <see cref="IBus"/> to extend.</param>
        /// <param name="address">
        /// The address to which the message will be sent.
        /// </param>
        /// <param name="message">The message to send.</param>
        public static ICallback Send(this IBus bus, MsmqAddress address, object message)
        {
            return bus.Send(address.ToString(), message);
        }

        /// <summary>
        /// Instantiates a message of type T and sends it to the given address.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface</typeparam>
        /// <param name="bus">The <see cref="IBus"/> to extend.</param>
        /// <param name="address">The address to which the message will be sent.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message</param>
        public static ICallback Send<T>(this IBus bus, MsmqAddress address, Action<T> messageConstructor)
        {
            return bus.Send(address.ToString(), messageConstructor);
        }

        /// <summary>
        /// Sends the message to the given address as well as identifying this
        /// as a response to a message containing the Id found in correlationId.
        /// </summary>
        public static ICallback Send(this IBus bus, MsmqAddress address, string correlationId, object message)
        {
            return bus.Send(address.ToString(), correlationId, message);
        }

        /// <summary>
        /// Instantiates a message of the type T using the given messageConstructor,
        /// and sends it to the given address identifying it as a response to a message
        /// containing the Id found in correlationId.
        /// </summary>
        public static ICallback Send<T>(this IBus bus, MsmqAddress address, string correlationId, Action<T> messageConstructor)
        {
            return bus.Send(address.ToString(), correlationId, messageConstructor);
        }

    }
}