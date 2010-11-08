using System;

namespace NServiceBus
{
    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    public interface IStartableBus : IDisposable
    {		
        /// <summary>
        /// Performs the given startup action, starts the bus, and returns a reference to it.
        /// </summary>
        /// <param name="startupAction">Action to be performed before the bus is started.</param>
        /// <returns>A reference to the bus.</returns>
        IBus Start(Action startupAction);

        /// <summary>
        /// Starts the bus and returns a reference to it.
        /// </summary>
        /// <returns></returns>
        IBus Start();

        /// <summary>
        /// Event raised when the bus is started.
        /// </summary>
        event EventHandler Started;
    }
}
