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
        /// <param name="instanceName">Instance name.</param>
        public static UnicastRoutingTarget ToEndpointInstance(EndpointInstanceName instanceName)
        {
            Guard.AgainstNull(nameof(instanceName), instanceName);
            return new UnicastRoutingTarget(instanceName.EndpointName, instanceName, null);
        }

        /// <summary>
        /// Creates a new destination to an anonymous instance of a known endpoint.
        /// </summary>
        /// <param name="endpoint">Endpoint name.</param>
        /// <param name="transportAddress">Instance transport address.</param>
        public static UnicastRoutingTarget ToAnonymousInstance(EndpointName endpoint, string transportAddress)
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

        UnicastRoutingTarget(EndpointName endpointName, EndpointInstanceName instanceName, string transportAddress)
        {
            EndpointName = endpointName;
            InstanceName = instanceName;
            TransportAddress = transportAddress;
        }

        internal string Resolve(Func<EndpointInstanceName, string> transportAddressResolver)
        {
            return TransportAddress ?? transportAddressResolver(InstanceName);
        }

        /// <summary>
        /// Endpoint name, if specified.
        /// </summary>
        public EndpointName EndpointName { get; }

        /// <summary>
        /// Endpoint instance name, if specified.
        /// </summary>
        public EndpointInstanceName InstanceName { get; }

        /// <summary>
        /// Endpoint instance transport address, if specified.
        /// </summary>
        public string TransportAddress { get; }

        bool Equals(UnicastRoutingTarget other)
        {
            return Equals(EndpointName, other.EndpointName) && Equals(InstanceName, other.InstanceName) && string.Equals(TransportAddress, other.TransportAddress);
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
                var hashCode = EndpointName?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (InstanceName?.GetHashCode() ?? 0);
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