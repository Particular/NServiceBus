namespace NServiceBus
{
    using System;

    public static partial class ISendOnlyBusExtensions
    {

        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="message">The message to publish.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "PublishAsync(object message)")]
        public static void Publish(this ISendOnlyBus bus, object message)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Publish the message to subscribers.
        /// </summary>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <typeparam name="T">The message type.</typeparam>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "PublishAsync()")]
        public static void Publish<T>(this ISendOnlyBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and publishes it.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">The instance of <see cref="ISendOnlyBus"/> to use for the action.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "PublishAsyn<T>(Action<T> messageConstructor)")]
        public static void Publish<T>(this ISendOnlyBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }
}