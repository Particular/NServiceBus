namespace NServiceBus
{
    /// <summary>
    /// Represents a logical address (independent of transport) of a local queue.
    /// </summary>
    public sealed class LocalAddress
    {
        /// <summary>
        /// Creates new local address for the provided endpoint instance name.
        /// </summary>
        /// <param name="endpoint">The logical name of the endpoint.</param>
        /// <param name="qualifier">The qualifier to apply to the given logical endpoint name.</param>
        /// <param name="discriminator">The discriminator to apply to the given logical endpoint name.</param>
        public LocalAddress(string endpoint, string qualifier = null, string discriminator = null)
        {
            Endpoint = endpoint;
            Qualifier = qualifier;
            Discriminator = discriminator;
        }

        /// <summary>
        /// Returns the qualifier of the local address.
        /// </summary>
        /// <returns>The configured qualifier or <code>null</code> when no qualifier is specified.</returns>
        public string Qualifier { get; }

        /// <summary>
        /// Returns the discriminator of the localaddress.
        /// </summary>
        /// <returns>The configured discriminator or <code>null</code> when no discriminator is specified.</returns>
        public string Discriminator { get; }

        /// <summary>
        /// Returns the logical endpoint name excluding qualifier and discriminator.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            var name = Endpoint;

            if (Discriminator != null)
            {
                name += $"-{Discriminator}";
            }

            if (Qualifier != null)
            {
                name += $".{Qualifier}";
            }

            return name;
        }

        bool Equals(LocalAddress other)
        {
            return string.Equals(Qualifier, other.Qualifier) && string.Equals(Discriminator, other.Discriminator) && string.Equals(Endpoint, other.Endpoint);
        }

        /// <summary>Determines whether the specified object is equal to the current object.</summary>
        /// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
        /// <param name="obj">The object to compare with the current object. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is LocalAddress && Equals((LocalAddress) obj);
        }

        /// <summary>Serves as the default hash function. </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (Qualifier != null ? Qualifier.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Discriminator != null ? Discriminator.GetHashCode() : 0);
                hashCode = (hashCode*397) ^ (Endpoint != null ? Endpoint.GetHashCode() : 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Compares for equality.
        /// </summary>
        public static bool operator ==(LocalAddress left, LocalAddress right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Compares for inequality.
        /// </summary>
        public static bool operator !=(LocalAddress left, LocalAddress right)
        {
            return !Equals(left, right);
        }
    }
}