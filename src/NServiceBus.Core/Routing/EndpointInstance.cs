namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents a name of an endpoint instance.
    /// </summary>
    public sealed class EndpointInstance
    {
        /// <summary>
        /// Creates a new endpoint name for a given discriminator.
        /// </summary>
        /// <param name="endpoint">The name of the endpoint.</param>
        /// <param name="discriminator">A specific discriminator for scale-out purposes.</param>
        /// <param name="properties">A bag of additional properties that differentiate this endpoint instance from other instances.</param>
        public EndpointInstance(string endpoint, string discriminator = null, IReadOnlyDictionary<string, string> properties = null)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);

            Properties = properties ?? new Dictionary<string, string>();
            Endpoint = endpoint;
            Discriminator = discriminator;
        }

        /// <summary>
        /// Returns the name of the endpoint.
        /// </summary>
        public string Endpoint { get; }

        /// <summary>
        /// A specific discriminator for scale-out purposes.
        /// </summary>
        public string Discriminator { get; }

        /// <summary>
        /// Returns all the differentiating properties of this instance.
        /// </summary>
        public IReadOnlyDictionary<string, string> Properties { get; }

        /// <summary>
        /// Sets a property for an endpoint instance returning a new instance with the given property set.
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public EndpointInstance SetProperty(string key, string value)
        {
            Guard.AgainstNull(nameof(key), key);
            var newProperties = new Dictionary<string, string>();
            foreach (var property in Properties)
            {
                newProperties[property.Key] = property.Value;
            }
            newProperties[key] = value;
            return new EndpointInstance(Endpoint, Discriminator, newProperties);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        public override string ToString()
        {
            var propsFormatted = Properties.Select(kvp => $"{kvp.Key}:{kvp.Value}");
            var instanceId = Discriminator != null
                ? $"{Endpoint}-{Discriminator}"
                : Endpoint;

            var parts = new[]
            {
                instanceId
            }.Concat(propsFormatted);
            return string.Join(";", parts);
        }

        bool Equals(EndpointInstance other)
        {
            return PropertiesEqual(Properties, other.Properties)
                   && Equals(Endpoint, other.Endpoint)
                   && string.Equals(Discriminator, other.Discriminator);
        }

        static bool PropertiesEqual(IReadOnlyDictionary<string, string> left, IReadOnlyDictionary<string, string> right)
        {
            foreach (var p in left)
            {
                string equivalent;
                if (!right.TryGetValue(p.Key, out equivalent))
                {
                    return false;
                }
                if (p.Value != equivalent)
                {
                    return false;
                }
            }
            return true;
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
            return obj is EndpointInstance && Equals((EndpointInstance) obj);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        /// <returns>
        /// A hash code for the current object.
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Properties.Aggregate(Endpoint.GetHashCode(), (i, pair) => (i*397) ^ propertyComparer.GetHashCode(pair));
                hashCode = (hashCode*397) ^ (Discriminator?.GetHashCode() ?? 0);
                return hashCode;
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        public static bool operator ==(EndpointInstance left, EndpointInstance right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Checks for inequality.
        /// </summary>
        public static bool operator !=(EndpointInstance left, EndpointInstance right)
        {
            return !Equals(left, right);
        }

        static readonly IEqualityComparer<KeyValuePair<string, string>> propertyComparer = new PropertyComparer();

        class PropertyComparer : IEqualityComparer<KeyValuePair<string, string>>
        {
            public bool Equals(KeyValuePair<string, string> x, KeyValuePair<string, string> y)
            {
                return Equals(x.Key, y.Key)
                       && Equals(x.Value, y.Value);
            }

            public int GetHashCode(KeyValuePair<string, string> obj)
            {
                var hashCode = obj.Key.GetHashCode();
                if (obj.Value != null)
                {
                    hashCode ^= 397*obj.Value.GetHashCode();
                }
                return hashCode;
            }
        }
    }
}