namespace NServiceBus
{
    using System;

    class TimeoutManagerAddressConfiguration
    {
        internal TimeoutManagerAddressConfiguration(string defaultTimeoutManagerAddress)
        {
            TransportAddress = defaultTimeoutManagerAddress;
        }

        public string TransportAddress { get; private set; }

        public void Set(string newTimeoutManagerAddress)
        {
            Guard.AgainstNullAndEmpty(newTimeoutManagerAddress, "newTimeoutManagerAddress");
            if (TransportAddress != null)
            {
                throw new InvalidOperationException("Another feature or the UnicastBusConfig section has already set the timeout manager address.");
            }
            TransportAddress = newTimeoutManagerAddress;
        }
    }
}