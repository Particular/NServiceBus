namespace NServiceBus
{
    using System;

    /// <summary>
    /// The interface used for starting and stopping an IBus.
    /// </summary>
    public interface IStartableBus : IBus, IDisposable
    {
        /// <summary>
        /// Starts the bus and returns a reference to it.
        /// </summary>
        /// <returns>A reference to the bus.</returns>
        IBus Start();

        /// <summary>
        /// Performs the shutdown of the current <see cref="IBus"/>.
        /// </summary>
        void Shutdown();

    }
}