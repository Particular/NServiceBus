namespace NServiceBus.Routing
{
    /// <summary>
    /// Represents a name of a logical endpoint.
    /// </summary>
    public sealed class EndpointName
    {
        /// <summary>
        /// Creates a new logical endpoint name.
        /// </summary>
        public EndpointName(string name)
        {
            this.name = name;
        }

        /// <summary>
        /// Returns the string representation of the endpoint name.
        /// </summary>
        public override string ToString()
        {
            return name;
        }

        bool Equals(EndpointName other)
        {
            return string.Equals(name, other.name);
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
            return obj is EndpointName && Equals((EndpointName) obj);
        }

        /// <summary>
        /// Serves as a hash function for a particular type.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            return name?.GetHashCode() ?? 0;
        }

        /// <summary>
        /// Tests for equality.
        /// </summary>
        public static bool operator ==(EndpointName left, EndpointName right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Tests for inequality.
        /// </summary>
        public static bool operator !=(EndpointName left, EndpointName right)
        {
            return !Equals(left, right);
        }

        string name;
    }
}