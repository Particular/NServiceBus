namespace NServiceBus
{
    using System;

    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    public interface IStartableBus
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
        /// <returns>A reference to the bus.</returns>
        IBus Start();


        /// <summary>
        /// Starts the bus in send only mode
        /// </summary>
        /// <returns>A reference to the bus.</returns>
        ISendOnlyBus StartInSendOnlyMode();

        /// <summary>
        /// Performs the shutdown of the current <see cref="IBus"/>.
        /// </summary>
        //todo: obsolete
        void Shutdown();

        /// <summary>
        /// Event raised when the bus is started.
        /// </summary>
        //todo: obsolete
        event EventHandler Started;
    }
}