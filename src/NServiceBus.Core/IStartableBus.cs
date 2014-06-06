namespace NServiceBus
{
    using System;
    using Installation;
    using ObjectBuilder;

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
        /// Runs all <see cref="INeedToInstallSomething"/> that a registered in the current <see cref="IBuilder"/>.
        /// </summary>
        void RunInstallers();

        /// <summary>
        /// Performs the shutdown of the current <see cref="IBus"/>.
        /// </summary>
        void Shutdown();

    }
}