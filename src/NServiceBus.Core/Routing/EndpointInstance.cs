namespace NServiceBus.Routing
{
    using System.Collections.Generic;

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
        /// <param name="address">A specific address for this instance.</param>
        /// <param name="properties">A bag of additional properties that differentiate this endpoint instance from other instances.</param>
        public EndpointInstance(string endpoint, string discriminator = null, string address = null, IReadOnlyDictionary<string, string> properties = null)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);

            Properties = properties ?? new Dictionary<string, string>();
            Endpoint = endpoint;
            Discriminator = discriminator;
            Address = address;
        }

        /// <summary>
        /// The address of the instance.
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// The name of the logical endpoint.
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
            return new EndpointInstance(Endpoint, Discriminator, Address, newProperties);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        public override string ToString()
        {
            return Address ?? Endpoint + (string.IsNullOrWhiteSpace(Discriminator) ? string.Empty : $"-{Discriminator}");
        }
    }
}