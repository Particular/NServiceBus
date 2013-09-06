namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using ObjectBuilder;

    /// <summary>
    /// Returns the action to dispatch the given message to the handler
    /// </summary>
    public interface IMessageDispatcherFactory
    {
        /// <summary>
        /// Returns the action that will dispatch this message
        /// </summary>
        /// <param name="messageHandlerType"></param>
        /// <param name="builder"></param>
        /// <param name="toHandle"></param>
        /// <returns></returns>
        IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object toHandle);

        /// <summary>
        /// Returns true if the factory is able to dispatch this type
        /// </summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        bool CanDispatch(Type handler);
    }
}