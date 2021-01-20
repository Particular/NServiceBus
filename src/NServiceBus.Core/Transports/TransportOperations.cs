namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Represents a set of transport operations.
    /// </summary>
    public class TransportOperations
    {
        /// <summary>
        /// Creates a new set of dispatchable transport operations.
        /// </summary>
        public TransportOperations(params TransportOperation[] transportOperations)
        {
            var multicastOperations = new List<MulticastTransportOperation>(transportOperations.Length);
            var unicastOperations = new List<UnicastTransportOperation>(transportOperations.Length);

            foreach (var transportOperation in transportOperations)
            {
                if (transportOperation.AddressTag is MulticastAddressTag multicastAddressTag)
                {
                    multicastOperations.Add(new MulticastTransportOperation(
                        transportOperation.Message,
                        multicastAddressTag.MessageType,
                        transportOperation.Properties,
                        transportOperation.RequiredDispatchConsistency));
                }
                else if (transportOperation.AddressTag is UnicastAddressTag unicastAddressTag)
                {
                    unicastOperations.Add(new UnicastTransportOperation(
                        transportOperation.Message,
                        unicastAddressTag.Destination,
                        transportOperation.Properties,
                        transportOperation.RequiredDispatchConsistency));
                }
                else
                {
                    throw new ArgumentException(
                        $"Transport operations contain an unsupported type of {nameof(AddressTag)}: {transportOperation.AddressTag.GetType().Name}. Supported types are {nameof(UnicastAddressTag)} and {nameof(MulticastAddressTag)}",
                        nameof(transportOperations));
                }
            }

            MulticastTransportOperations = multicastOperations;
            UnicastTransportOperations = unicastOperations;
        }

        /// <summary>
        /// A list of multicast transport operations.
        /// </summary>
        public List<MulticastTransportOperation> MulticastTransportOperations { get; }

        /// <summary>
        /// A list of unicast transport operations.
        /// </summary>
        public List<UnicastTransportOperation> UnicastTransportOperations { get; }
    }
}