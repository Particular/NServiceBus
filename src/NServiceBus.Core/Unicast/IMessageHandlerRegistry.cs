namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The registry that keeps track of all known message handlers
    /// </summary>
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
    }
}