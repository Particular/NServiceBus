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
        /// Gets the list of messagehandlers for the given message type
        /// </summary>
        /// <param name="messageType"></param>
        /// <returns></returns>
        IEnumerable<Type> GetHandlerTypes(Type messageType);

        /// <summary>
        /// Lists all message type for which we have handlers
        /// </summary>
        /// <returns></returns>
        IEnumerable<Type> GetMessageTypes();
    }
}