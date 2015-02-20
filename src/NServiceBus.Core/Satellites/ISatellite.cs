namespace NServiceBus.Satellites
{
    /// <summary>
    /// Implement this interface to create a Satellite.
    /// </summary>
    public interface ISatellite
    {
        /// <summary>
        /// This method is called when a message is available to be processed.
        /// </summary>
        /// <param name="message">The <see cref="TransportMessage"/> received.</param>
        /// <returns>If <code>false</code> then the message is not commited (rollback).</returns>
        bool Handle(TransportMessage message);

        /// <summary>
        /// The <see cref="Address"/> for this <see cref="ISatellite"/> to use when receiving messages.
        /// </summary>
        Address InputAddress { get; }

        /// <summary>
        /// Set to <code>true</code> to disable this <see cref="ISatellite"/>.
        /// </summary>
        bool Disabled { get; }

        /// <summary>
        /// Starts the <see cref="ISatellite"/>.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the <see cref="ISatellite"/>.
        /// </summary>
        void Stop();
    }
}
