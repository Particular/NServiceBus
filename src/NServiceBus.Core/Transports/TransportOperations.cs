namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using NServiceBus.Routing;

    /// <summary>
    /// Represents a set of transport operations.
    /// </summary>
    public class TransportOperations
    {
        internal TransportOperations(params TransportOperation[] transportOperations)
        {
            var multicastOperations = new List<MulticastTransportOperation>(transportOperations.Length);
            var unicastOperations = new List<UnicastTransportOperation>(transportOperations.Length);

            foreach (var transportOperation in transportOperations)
            {
                if (transportOperation.AddressTag is MulticastAddressTag)
                {
                    multicastOperations.Add(new MulticastTransportOperation(
                        transportOperation.Message, 
                        (MulticastAddressTag) transportOperation.AddressTag, 
                        transportOperation.DeliveryConstraints, 
                        transportOperation.RequiredDispatchConsistency));
                }
                else if (transportOperation.AddressTag is UnicastAddressTag)
                {
                    unicastOperations.Add(new UnicastTransportOperation(
                        transportOperation.Message,
                        (UnicastAddressTag) transportOperation.AddressTag,
                        transportOperation.DeliveryConstraints,
                        transportOperation.RequiredDispatchConsistency));
                }
                else
                {
                    throw new ArgumentException(
                        $"Transport operations contain an unsupported type of {typeof(AddressTag).Name}: {transportOperation.AddressTag.GetType().Name}. Supported types are {typeof(UnicastAddressTag).Name} and {typeof(MulticastAddressTag).Name}", 
                        nameof(transportOperations));
                }
            }

            MulticastTransportOperations = multicastOperations;
            UnicastTransportOperations = unicastOperations;
        }

        /// <summary>
        /// Creates a new set of dispatchable transport operations.
        /// </summary>
        public TransportOperations(IEnumerable<MulticastTransportOperation> multicastTransportOperations, IEnumerable<UnicastTransportOperation> unicastTransportOperations)
        {
            Guard.AgainstNull(nameof(multicastTransportOperations), multicastTransportOperations);
            Guard.AgainstNull(nameof(unicastTransportOperations), unicastTransportOperations);

            MulticastTransportOperations = multicastTransportOperations;
            UnicastTransportOperations = unicastTransportOperations;
        }

        /// <summary>
        /// A list of multicast transport operations.
        /// </summary>
        public IEnumerable<MulticastTransportOperation> MulticastTransportOperations { get; }

        /// <summary>
        /// A list of unicast transport operations.
        /// </summary>
        public IEnumerable<UnicastTransportOperation> UnicastTransportOperations { get; }
    }
}