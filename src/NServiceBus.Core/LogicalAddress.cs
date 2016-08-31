namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Routing;
    using static System.String;

    /// <summary>
    /// Represents a logical address (independent of transport).
    /// </summary>
    public struct LogicalAddress
    {
        IReadOnlyDictionary<string, string> properties;

        LogicalAddress(string queueName, string discriminator, string qualifier, IReadOnlyDictionary<string, string> properties)
        {
            this.properties = properties;
            QueueName = queueName;
            Discriminator = discriminator;
            Qualifier = qualifier;
        }

        /// <summary>
        /// Creates a logical address for a remote endpoint.
        /// </summary>
        /// <param name="endpointInstance">The endpoint instance that describes the remote endpoint.</param>
        public static LogicalAddress CreateRemoteAddress(EndpointInstance endpointInstance)
        {
            Guard.AgainstNull(nameof(endpointInstance), endpointInstance);
            return new LogicalAddress(endpointInstance.Endpoint, endpointInstance.Discriminator, null, endpointInstance.Properties);
        }

        /// <summary>
        /// Creates a logical address for this endpoint.
        /// </summary>
        /// <param name="queueName">The name of the main input queue.</param>
        /// <param name="properties">The additional transport-specific properties.</param>
        public static LogicalAddress CreateLocalAddress(string queueName, IReadOnlyDictionary<string, string> properties)
        {
            Guard.AgainstNullAndEmpty(nameof(queueName), queueName);
            Guard.AgainstNull(nameof(properties), properties);
            return new LogicalAddress(queueName, null, null, properties);
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
            if (Discriminator != null)
            {
                throw new Exception("Cannot add a qualifier to an individualized address.");
            }
            return new LogicalAddress(QueueName, null, qualifier, properties);
        }


        /// <summary>
        /// Creates a new individualized logical address with the specified discriminator.
        /// </summary>
        /// <param name="discriminator">The discriminator value used to individualize the address.</param>
        public LogicalAddress CreateIndividualizedAddress(string discriminator)
        {
            Guard.AgainstNullAndEmpty(nameof(discriminator), discriminator);
            if (Discriminator != null)
            {
                throw new Exception("Cannot add a discriminator to an already individualized address.");
            }
            if (Qualifier != null)
            {
                throw new Exception("Cannot add a discriminator to a qualified address.");
            }
            return new LogicalAddress(QueueName, discriminator, null, properties);
        }

        /// <summary>
        /// Returns the qualifier, or null if the address isn't qualified.
        /// </summary>
        public string Qualifier { get; }

        /// <summary>
        /// Returns the endpoint instance.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Returns the discriminator for the queue.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Gets a property value.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        /// <param name="value">Value of the property.</param>
        public bool TryGetProperty(string propertyName, out string value)
        {
            Guard.AgainstNull(nameof(propertyName), propertyName);
            return properties.TryGetValue(propertyName, out value);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var propsFormatted = properties.Select(kvp => $"{kvp.Key}:{kvp.Value}");
            var queueNameBuilder = new StringBuilder(QueueName);
            if (Discriminator != null)
            {
                queueNameBuilder.Append("-" + Discriminator);
            }
            if (Qualifier != null)
            {
                queueNameBuilder.Append("." + Qualifier);
            }
            var parts = new[]
            {
                queueNameBuilder.ToString()
            }.Concat(propsFormatted);
            return Join(";", parts);
        }

        bool Equals(LogicalAddress other)
        {
            return PropertiesEqual(properties, other.properties)
                   && string.Equals(Qualifier, other.Qualifier, StringComparison.Ordinal)
                   && string.Equals(QueueName, other.QueueName, StringComparison.Ordinal)
                   && string.Equals(Discriminator, other.Discriminator, StringComparison.Ordinal);
        }

        /// <summary>Indicates whether this instance and a specified object are equal.</summary>
        /// <returns>
        /// true if <paramref name="obj" /> and this instance are the same type and represent the same value; otherwise,
        /// false.
        /// </returns>
        /// <param name="obj">The object to compare with the current instance. </param>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is LogicalAddress && Equals((LogicalAddress) obj);
        }

        /// <summary>Returns the hash code for this instance.</summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = PropertiesHashcode(properties);
                hashCode = (hashCode*397) ^ QueueName.GetHashCode();
                hashCode = (hashCode*397) ^ (Qualifier?.GetHashCode() ?? 0);
                hashCode = (hashCode*397) ^ (Discriminator?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        static int PropertiesHashcode(IReadOnlyDictionary<string, string> props)
        {
            var hashCode = 0;
            foreach (var kvp in props.OrderBy(kvp => kvp.Key))
            {
                hashCode = (hashCode*397) ^ kvp.Key.GetHashCode();
                hashCode = (hashCode*397) ^ (kvp.Value?.GetHashCode() ?? 0);
            }
            return hashCode;
        }

        static bool PropertiesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }
            foreach (var p in left)
            {
                string equivalent;
                if (!right.TryGetValue(p.Key, out equivalent))
                {
                    return false;
                }
                if (!string.Equals(p.Value, equivalent, StringComparison.Ordinal))
                {
                    return false;
                }
            }
            return true;
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