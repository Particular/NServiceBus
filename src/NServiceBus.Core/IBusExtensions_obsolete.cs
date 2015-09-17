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
        
        /// <summary>
        /// Sends the message back to the current bus.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="message">The message to send.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "SendLocalAsync(object message)")]
        public static void SendLocal(this IBus bus, object message)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Instantiates a message of type T and sends it back to the current bus.
        /// </summary>
        /// <typeparam name="T">The type of message, usually an interface.</typeparam>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageConstructor">An action which initializes properties of the message.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "SendLocalAsync<T>(Action<T> messageConstructor)")]
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "HandleCurrentMessageLaterAsync()")]
        public static void HandleCurrentMessageLater(this IBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "ForwardCurrentMessageToAsync(string destination)")]
        public static void ForwardCurrentMessageTo(this IBus bus, string destination)
        {
            throw new NotImplementedException();
        }
    }
}