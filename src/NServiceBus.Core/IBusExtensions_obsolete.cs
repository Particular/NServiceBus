namespace NServiceBus
{
    using System;

    public static partial class IBusExtensions
    {
        /// <summary>
        /// Sends the message to the endpoint which sent the message currently being handled on this thread.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ReplyAsync(object message)")]
        public static void Reply(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and performs a regular ReplyAsync.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ReplyAsync<T>(Action<T> messageConstructor)")]
        public static void Reply<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }
    }
}