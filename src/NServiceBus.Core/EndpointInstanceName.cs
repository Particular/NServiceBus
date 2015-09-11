namespace NServiceBus
{
    using System.Text;

    /// <summary>
    /// Represents a name of an endpoint instance.
    /// </summary>
    public sealed class EndpointInstanceName
    {
        readonly EndpointName endpointName;
        readonly string userDiscriminator;
        readonly string transportDiscriminator;

        /// <summary>
        /// Creates a new endpoint name for a given discriminator.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="userDiscriminator">The discriminator provided by the user, if any.</param>
        /// <param name="transportDiscriminator">The discriminator provided by the transport, if any.</param>
        public EndpointInstanceName(EndpointName endpointName, string userDiscriminator, string transportDiscriminator)
        {
            this.endpointName = endpointName;
            this.userDiscriminator = userDiscriminator;
            this.transportDiscriminator = transportDiscriminator;
        }

        /// <summary>
        /// Returns the name of the endpoint.
        /// </summary>
        public EndpointName EndpointName
        {
            get { return endpointName; }
        }

        /// <summary>
        /// The discriminator provided by the user, if any.
        /// </summary>
        public string UserDiscriminator
        {
            get { return userDiscriminator; }
        }

        /// <summary>
        /// The discriminator provided by the transport, if any.
        /// </summary>
        public string TransportDiscriminator
        {
            get { return transportDiscriminator; }
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder(endpointName.ToString());
            if (userDiscriminator != null)
            {
                builder.Append("-" + userDiscriminator);
            }
            if (transportDiscriminator != null)
            {
                builder.Append("-" + transportDiscriminator);
            }
            return builder.ToString();
        }


        bool Equals(EndpointInstanceName other)
        {
            return Equals(endpointName, other.endpointName) && string.Equals(userDiscriminator, other.userDiscriminator) && string.Equals(transportDiscriminator, other.transportDiscriminator);
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
            return obj is EndpointInstanceName && Equals((EndpointInstanceName) obj);
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
                var hashCode = (endpointName != null ? endpointName.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (userDiscriminator != null ? userDiscriminator.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (transportDiscriminator != null ? transportDiscriminator.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(EndpointInstanceName left, EndpointInstanceName right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(EndpointInstanceName left, EndpointInstanceName right)
        {
            return !Equals(left, right);
        }
    }

}