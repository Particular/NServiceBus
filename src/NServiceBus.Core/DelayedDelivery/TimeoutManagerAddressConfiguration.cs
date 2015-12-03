namespace NServiceBus
{
    using System;

    /// <summary>
    /// Allows to configure the timeout address.
    /// </summary>
    class TimeoutManagerAddressConfiguration
    {
        internal TimeoutManagerAddressConfiguration(string defaultTimeoutManagerAddress)
        {
            TransportAddress = defaultTimeoutManagerAddress;
        }

        /// <summary>
        /// Sets the address of the timeout manager.
        /// </summary>
        public void Set(string newTimeoutManagerAddress)
        {
            Guard.AgainstNullAndEmpty(newTimeoutManagerAddress, "newTimeoutManagerAddress");
            if (TransportAddress != null)
            {
                throw new InvalidOperationException("Another feature or the UnicastBusConfig section has already set the timeout manager address.");
            }
            TransportAddress = newTimeoutManagerAddress;
        }

        public string TransportAddress { get; private set; }
    }
}