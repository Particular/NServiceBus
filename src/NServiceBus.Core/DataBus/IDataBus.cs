namespace NServiceBus.DataBus
{
    using System;
    using System.IO;

    /// <summary>
    /// The main interface for interactions with the databus.
    /// </summary>
    public interface IDataBus
    {
        /// <summary>
        /// Gets a data item from the bus.
        /// </summary>
        /// <param name="key">The key to look for.</param>
        /// <returns>The data <see cref="Stream"/>.</returns>
        Stream Get(string key);

        /// <summary>
        /// Adds a data item to the bus and returns the assigned key.
        /// </summary>
        /// <param name="stream">A create containing the data to be sent on the databus.</param>
        /// <param name="timeToBeReceived">The time to be received specified on the message type. TimeSpan.MaxValue is the default.</param>
        string Put(Stream stream, TimeSpan timeToBeReceived);

        /// <summary>
        /// Called when the bus starts up to allow the data bus to active background tasks.
        /// </summary>
        void Start();
    }
}