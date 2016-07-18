namespace NServiceBus.Routing
{
    /// <summary>
    /// Represents a name of an endpoint instance.
    /// </summary>
    public sealed class EndpointInstance
    {
        /// <summary>
        /// Creates a new endpoint name for a given discriminator.
        /// </summary>
        /// <param name="endpoint">The name of the endpoint.</param>
        public EndpointInstance(string endpoint) : this(endpoint, endpoint)
        {
        }

        /// <summary>
        /// Creates a new endpoint name for a given discriminator.
        /// </summary>
        /// <param name="endpoint">The name of the endpoint.</param>
        /// <param name="instanceName">The name of the this instance.</param>
        public EndpointInstance(string endpoint, string instanceName)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            Guard.AgainstNull(nameof(instanceName), instanceName);

            Endpoint = endpoint;
            InstanceName = instanceName;
        }

        /// <summary>
        /// Returns the name of the endpoint.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// The configured instance name.
        /// </summary>
        public string InstanceName { get; }

        bool Equals(EndpointInstance other)
        {
            return string.Equals(Endpoint, other.Endpoint) && string.Equals(InstanceName, other.InstanceName);
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object.</param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is EndpointInstance && Equals((EndpointInstance) obj);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Endpoint != null ? Endpoint.GetHashCode() : 0)*397) ^ (InstanceName != null ? InstanceName.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Determines whether the specified object is equal to the current object.
        /// </summary>
        public static bool operator ==(EndpointInstance left, EndpointInstance right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether the specified object is not equal to the current object.
        /// </summary>
        public static bool operator !=(EndpointInstance left, EndpointInstance right)
        {
            return !Equals(left, right);
        }
    }
}