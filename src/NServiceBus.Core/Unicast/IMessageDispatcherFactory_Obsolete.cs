namespace NServiceBus.Unicast
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.ObjectBuilder;

    /// <summary>
    /// Returns the action to dispatch the given message to the handler
    /// </summary>
    [ObsoleteEx(RemoveInVersion = "6.0", TreatAsErrorFromVersion = "5.0", Replacement = "Use the pipeline and replace the InvokeHandlers step")]
    public interface IMessageDispatcherFactory
    {
        /// <summary>
        /// Returns the action that will dispatch this message
        /// </summary>
        IEnumerable<Action> GetDispatcher(Type messageHandlerType, IBuilder builder, object toHandle);

        /// <summary>
        /// Returns true if the factory is able to dispatch this type
        /// </summary>
        bool CanDispatch(Type handler);
    }
}
