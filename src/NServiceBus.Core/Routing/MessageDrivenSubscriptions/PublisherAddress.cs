namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Represents an address of a publisher.
    /// </summary>
    public class PublisherAddress
    {
        /// <summary>
        /// Creates a new publisher based on the endpoint name.
        /// </summary>
        public static PublisherAddress CreateFromEndpointName(string endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            return new PublisherAddress { endpoint = endpoint };
        }

        /// <summary>
        /// Creates a new publisher based on a set of endpoint instance names.
        /// </summary>
        public static PublisherAddress CreateFromEndpointInstances(params EndpointInstance[] instances)
        {
            Guard.AgainstNull(nameof(instances), instances);
            if (instances.Length == 0)
            {
                throw new ArgumentException("You have to provide at least one instance.");
            }
            return new PublisherAddress { instances = instances };
        }

        /// <summary>
        /// Creates a new publisher based on a set of physical addresses.
        /// </summary>
        public static PublisherAddress CreateFromPhysicalAddresses(params string[] addresses)
        {
            Guard.AgainstNull(nameof(addresses), addresses);
            if (addresses.Length == 0)
            {
                throw new ArgumentException("You need to provide at least one address.");
            }
            return new PublisherAddress { addresses = addresses };
        }

        private PublisherAddress()
        {
        }

        internal IEnumerable<string> Resolve(Func<string, IEnumerable<EndpointInstance>> instanceResolver, Func<EndpointInstance, string> addressResolver)
        {
            if (addresses != null)
            {
                return addresses;
            }
            if (instances != null)
            {
                return instances.Select(addressResolver);
            }
            var result = instanceResolver(endpoint);
            return result.Select(addressResolver);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            if (endpoint != null)
            {
                return endpoint;
            }
            if (instances != null)
            {
                return string.Join(", ", instances.Select(x => $"[{x.ToString()}]").OrderBy(x => x));
            }
            return string.Join(", ", addresses.Select(x => $"<{x}>").OrderBy(x => x));
        }

        string[] addresses;
        string endpoint;
        EndpointInstance[] instances;
    }
}