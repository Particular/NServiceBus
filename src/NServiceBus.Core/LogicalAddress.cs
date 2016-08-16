namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using Routing;

    /// <summary>
    /// Represents a logical address (independent of transport).
    /// </summary>
    public struct LogicalAddress
    {
        LogicalAddress(EndpointInstance endpointInstance, string qualifier)
        {
            EndpointInstance = endpointInstance;
            Qualifier = qualifier;
        }

        /// <summary>
        /// Creates a logical address for a remote endpoint.
        /// </summary>
        /// <param name="endpointInstance">The endpoint instance that describes the remote endpoint.</param>
        public static LogicalAddress CreateRemoteAddress(EndpointInstance endpointInstance)
        {
            return new LogicalAddress(endpointInstance, null);
        }

        /// <summary>
        /// Creates a logical address for this endpoint.
        /// </summary>
        /// <param name="queueName">The name of the main input queue.</param>
        /// <param name="properties">The additional transport-specific properties.</param>
        public static LogicalAddress CreateLocalAddress(string queueName, IReadOnlyDictionary<string, string> properties)
        {
            return new LogicalAddress(new EndpointInstance(queueName, null, properties), null);
        }

        /// <summary>
        /// Creates a new logical address with the given qualifier.
        /// </summary>
        /// <param name="qualifier">The qualifier for the new address.</param>
        public LogicalAddress CreateQualifiedAddress(string qualifier)
        {
            Guard.AgainstNullAndEmpty(nameof(qualifier), qualifier);
            if (Qualifier != null)
            {
                throw new Exception("Cannot add a qualifier to an already qualified address.");
            }
            if (EndpointInstance.Discriminator != null)
            {
                throw new Exception("Cannot add a qualifier to an individualized address.");
            }
            return new LogicalAddress(EndpointInstance, qualifier);
        }


        /// <summary>
        /// Creates a new individualized logical address with the specified discriminator.
        /// </summary>
        /// <param name="discriminator">The discriminator value used to individualize the address.</param>
        public LogicalAddress CreateIndividualizedAddress(string discriminator)
        {
            Guard.AgainstNullAndEmpty(nameof(discriminator), discriminator);
            if (EndpointInstance.Discriminator != null)
            {
                throw new Exception("Cannot add a discriminator to an already individualized address.");
            }
            if (Qualifier != null)
            {
                throw new Exception("Cannot add a discriminator to a qualified address.");
            }
            return new LogicalAddress(new EndpointInstance(EndpointInstance.Endpoint, discriminator, EndpointInstance.Properties), null);
        }

        /// <summary>
        /// Returns the qualifier, or null if the address isn't qualified.
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// Returns the endpoint instance.
        /// </summary>
        public EndpointInstance EndpointInstance { get; }

        bool Equals(LogicalAddress other)
        {
            return string.Equals(Qualifier, other.Qualifier) && Equals(EndpointInstance, other.EndpointInstance);
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
                return EndpointInstance + "." + Qualifier;
            }
            return EndpointInstance.ToString();
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
                return ((Qualifier?.GetHashCode() ?? 0)*397) ^ (EndpointInstance?.GetHashCode() ?? 0);
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