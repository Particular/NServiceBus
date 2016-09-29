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
                // ReSharper disable once CanBeReplacedWithTryCastAndCheckForNull
                if (transportOperation.AddressTag is MulticastAddressTag)
                {
                    multicastOperations.Add(new MulticastTransportOperation(
                        transportOperation.Message,
                        ((MulticastAddressTag) transportOperation.AddressTag).MessageType,
                        transportOperation.RequiredDispatchConsistency,
                        transportOperation.DeliveryConstraints));
                }
                else if (transportOperation.AddressTag is UnicastAddressTag)
                {
                    unicastOperations.Add(new UnicastTransportOperation(
                        transportOperation.Message,
                        ((UnicastAddressTag) transportOperation.AddressTag).Destination,
                        transportOperation.RequiredDispatchConsistency,
                        transportOperation.DeliveryConstraints));
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
        /// A list of multicast transport operations.
        /// </summary>
        public List<MulticastTransportOperation> MulticastTransportOperations { get; }

        /// <summary>
        /// A list of unicast transport operations.
        /// </summary>
        public List<UnicastTransportOperation> UnicastTransportOperations { get; }
    }
}