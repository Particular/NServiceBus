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
        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.ReplyAsync(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
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
            Message = "Please use `IMessageHandlerContext.ReplyAsync<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
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
            Message = "Please use `IMessageHandlerContext.SendLocalAsync(object message)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
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
            Message = "Please use `IMessageHandlerContext.SendLocalAsync<T>(Action<T> messageConstructor)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void SendLocal<T>(this IBus bus, Action<T> messageConstructor)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Moves the message being handled to the back of the list of available 
        /// messages so it can be handled later.
        /// </summary>
        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.HandleCurrentMessageLaterAsync()` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void HandleCurrentMessageLater(this IBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Forwards the current message being handled to the destination maintaining
        /// all of its transport-level properties and headers.
        /// </summary>
        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.ForwardCurrentMessageToAsync(string destination)` provided to message handlers instead.",
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7")]
        public static void ForwardCurrentMessageTo(this IBus bus, string destination)
        {
            throw new NotImplementedException();
        }

        /// <summary>		
        /// Tells the bus to stop dispatching the current message to additional		
        /// handlers.		
        /// </summary>
        [ObsoleteEx(
            Message = "Please use `IMessageHandlerContext.DoNotContinueDispatchingCurrentMessageToHandlers()` provided to message handlers instead.",
            RemoveInVersion = "7.0",
            TreatAsErrorFromVersion = "6.0")]
        public static void DoNotContinueDispatchingCurrentMessageToHandlers(this IBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes to receive published messages of the specified type.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "SubscribeAsync(Type messageType)")]
        public static void Subscribe(this IBus bus, Type messageType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Subscribes to receive published messages of type T.
        /// This method is only necessary if you turned off auto-subscribe.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to subscribe to.</typeparam>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "SubscribeAsync<T>()")]
        public static void Subscribe<T>(this IBus bus)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <param name="messageType">The type of message to subscribe to.</param>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "UnsubscribeAsync(Type messageType)")]
        public static void Unsubscribe(this IBus bus, Type messageType)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unsubscribes from receiving published messages of the specified type.
        /// </summary>
        /// <param name="bus">Object being extended.</param>
        /// <typeparam name="T">The type of message to unsubscribe from.</typeparam>
        [ObsoleteEx(
            TreatAsErrorFromVersion = "6",
            RemoveInVersion = "7",
            ReplacementTypeOrMember = "UnsubscribeAsync<T>()")]
        public static void Unsubscribe<T>(this IBus bus)
        {
            throw new NotImplementedException();
        }
    }
}