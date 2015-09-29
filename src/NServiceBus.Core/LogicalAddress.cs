namespace NServiceBus
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a logical address (independent of transport).
    /// </summary>
    public sealed class LogicalAddress
    {
        /// <summary>
        /// Creates new qualified logical address for the provided endpoint instance name.
        /// </summary>
        /// <param name="endpointInstanceName">The name of the instance.</param>
        /// <param name="qualifier">The qualifier of this address.</param>
        public LogicalAddress(EndpointInstanceName endpointInstanceName, [NotNull] string qualifier)
        {
            if (qualifier == null)
            {
                throw new ArgumentNullException("qualifier");
            }
            EndpointInstanceName = endpointInstanceName;
            Qualifier = qualifier;
        }

        /// <summary>
        /// Creates new root logical address for the provided endpoint instance name.
        /// </summary>
        /// <param name="endpointInstanceName">The name of the instance.</param>
        public LogicalAddress(EndpointInstanceName endpointInstanceName)
        {
            EndpointInstanceName = endpointInstanceName;
        }


        /// <summary>
        /// Returns the qualifier or null for the root logical address for a given instance name.
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// Returns the instance name.
        /// </summary>
        public EndpointInstanceName EndpointInstanceName { get; }

        bool Equals(LogicalAddress other)
        {
            return string.Equals(Qualifier, other.Qualifier) && Equals(EndpointInstanceName, other.EndpointInstanceName);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            if (Qualifier != null)
            {
                return EndpointInstanceName + "." + Qualifier;
            }
            return EndpointInstanceName.ToString();
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
            return obj is LogicalAddress && Equals((LogicalAddress) obj);
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
                return ((Qualifier != null ? Qualifier.GetHashCode() : 0)*397) ^ (EndpointInstanceName != null ? EndpointInstanceName.GetHashCode() : 0);
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(LogicalAddress left, LogicalAddress right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(LogicalAddress left, LogicalAddress right)
        {
            return !Equals(left, right);
        }
    }
}