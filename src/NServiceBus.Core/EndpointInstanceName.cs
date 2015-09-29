namespace NServiceBus
{
    using System.Text;

    /// <summary>
    /// Represents a name of an endpoint instance.
    /// </summary>
    public sealed class EndpointInstanceName
    {
        /// <summary>
        /// Creates a new endpoint name for a given discriminator.
        /// </summary>
        /// <param name="endpointName">The name of the endpoint.</param>
        /// <param name="userDiscriminator">The discriminator provided by the user, if any.</param>
        /// <param name="transportDiscriminator">The discriminator provided by the transport, if any.</param>
        public EndpointInstanceName(EndpointName endpointName, string userDiscriminator, string transportDiscriminator)
        {
            EndpointName = endpointName;
            UserDiscriminator = userDiscriminator;
            TransportDiscriminator = transportDiscriminator;
        }

        /// <summary>
        /// Returns the name of the endpoint.
        /// </summary>
        public EndpointName EndpointName { get; }

        /// <summary>
        /// The discriminator provided by the user, if any.
        /// </summary>
        public string UserDiscriminator { get; }

        /// <summary>
        /// The discriminator provided by the transport, if any.
        /// </summary>
        public string TransportDiscriminator { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var builder = new StringBuilder(EndpointName.ToString());
            if (UserDiscriminator != null)
            {
                builder.Append("-" + UserDiscriminator);
            }
            if (TransportDiscriminator != null)
            {
                builder.Append("-" + TransportDiscriminator);
            }
            return builder.ToString();
        }


        bool Equals(EndpointInstanceName other)
        {
            return Equals(EndpointName, other.EndpointName) && string.Equals(UserDiscriminator, other.UserDiscriminator) && string.Equals(TransportDiscriminator, other.TransportDiscriminator);
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
                var hashCode = EndpointName?.GetHashCode() ?? 0;
                hashCode = (hashCode*397) ^ (UserDiscriminator?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (TransportDiscriminator?.GetHashCode() ?? 0);
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