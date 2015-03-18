namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The registry that keeps track of all known message handlers
    /// </summary>
    [ObsoleteEx(
        RemoveInVersion = "7.0",
        TreatAsErrorFromVersion = "6.0",
        Message = "Not a public API",
        ReplacementTypeOrMember = "MessageHandlerRegistry")]
    public interface IMessageHandlerRegistry
    {
        /// <summary>
        /// Gets the list of <see cref="IHandleMessages{T}"/> <see cref="Type"/>s for the given <paramref name="messageType"/>
        /// </summary>
        IEnumerable<Type> GetHandlerTypes(Type messageType);

        /// <summary>
        /// Lists all message type for which we have handlers
        /// </summary>
        IEnumerable<Type> GetMessageTypes();

        /// <summary>
        /// Invokes the handle method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="message">The message instance.</param>
        void InvokeHandle(object handler, object message);

        /// <summary>
        /// Invokes the timeout method of the given handler passing the message
        /// </summary>
        /// <param name="handler">The handler instance.</param>
        /// <param name="state">The message instance.</param>
        void InvokeTimeout(object handler, object state);
    }
}