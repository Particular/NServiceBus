namespace NServiceBus
{
    using System;

    /// <summary>
    /// Represents a reference to an endpoint instance (which is either its name or a transport address).
    /// </summary>
    public sealed class UnicastRoutingTarget
    {
        /// <summary>
        /// Creates a enw destination to a known endpoint.
        /// </summary>
        /// <param name="instance">Instance name.</param>
        public static UnicastRoutingTarget ToEndpointInstance(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);
            return new UnicastRoutingTarget(instance.Endpoint, instance, null);
        }

        /// <summary>
        /// Creates a new destination to an anonymous instance of a known endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint name.</param>
        /// <param name="transportAddress">Instance transport address.</param>
        public static UnicastRoutingTarget ToAnonymousInstance(Endpoint endpoint, string transportAddress)
        {
            Guard.AgainstNull(nameof(endpoint), transportAddress);
            Guard.AgainstNull(nameof(transportAddress), transportAddress);
            return new UnicastRoutingTarget(endpoint, null, transportAddress);
        }

        /// <summary>
        /// Creates a new destination to a transport address.
        /// </summary>
        /// <param name="transportAddress">Transport address.</param>
        public static UnicastRoutingTarget ToTransportAddress(string transportAddress)
        {
            Guard.AgainstNull(nameof(transportAddress), transportAddress);
            return new UnicastRoutingTarget(null, null, transportAddress);
        }

        UnicastRoutingTarget(Endpoint endpoint, EndpointInstance instance, string transportAddress)
        {
            Endpoint = endpoint;
            Instance = instance;
            TransportAddress = transportAddress;
        }

        internal string Resolve(Func<EndpointInstance, string> transportAddressResolver)
        {
            return TransportAddress ?? transportAddressResolver(Instance);
        }

        /// <summary>
        /// Endpoint name, if specified.
        /// </summary>
        public Endpoint Endpoint { get; }

        /// <summary>
        /// Endpoint instance name, if specified.
        /// </summary>
        public EndpointInstance Instance { get; }

        /// <summary>
        /// Endpoint instance transport address, if specified.
        /// </summary>
        public string TransportAddress { get; }

        bool Equals(UnicastRoutingTarget other)
        {
            return Equals(Endpoint, other.Endpoint) && Equals(Instance, other.Instance) && string.Equals(TransportAddress, other.TransportAddress);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>
        /// true if the specified object  is equal to the current object; otherwise, false.
        /// </returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            if (ReferenceEquals(this, obj))
            {
                return true;
            }
            return obj is UnicastRoutingTarget && Equals((UnicastRoutingTarget) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type. 
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Endpoint?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (Instance?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (TransportAddress?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(UnicastRoutingTarget left, UnicastRoutingTarget right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(UnicastRoutingTarget left, UnicastRoutingTarget right)
        {
            return !Equals(left, right);
        }
    }
}