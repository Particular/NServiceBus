namespace NServiceBus
{
    using System;
    using JetBrains.Annotations;

    /// <summary>
    /// Represents a logical address (independent of transport).
    /// </summary>
    public sealed class LogicalAddress
    {
        readonly string qualifier;
        readonly EndpointInstanceName endpointInstanceName;

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
            this.endpointInstanceName = endpointInstanceName;
            this.qualifier = qualifier;
        }

        /// <summary>
        /// Creates new root logical address for the provided endpoint instance name.
        /// </summary>
        /// <param name="endpointInstanceName">The name of the instance.</param>
        public LogicalAddress(EndpointInstanceName endpointInstanceName)
        {
            this.endpointInstanceName = endpointInstanceName;
        }


        /// <summary>
        /// Returns the qualifier or null for the root logical address for a given instance name.
        /// </summary>
        public string Qualifier
        {
            get { return qualifier; }
        }

        /// <summary>
        /// Returns the instance name.
        /// </summary>
        public EndpointInstanceName EndpointInstanceName
        {
            get { return endpointInstanceName; }
        }

        bool Equals(LogicalAddress other)
        {
            return string.Equals(qualifier, other.qualifier) && Equals(endpointInstanceName, other.endpointInstanceName);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            if (qualifier != null)
            {
                return endpointInstanceName + "." + qualifier;
            }
            return endpointInstanceName.ToString();
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
                return ((qualifier != null ? qualifier.GetHashCode() : 0)*397) ^ (endpointInstanceName != null ? endpointInstanceName.GetHashCode() : 0);
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