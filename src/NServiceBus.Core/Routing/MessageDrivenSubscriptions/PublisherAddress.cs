namespace NServiceBus.Routing.MessageDrivenSubscriptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents an address of a publisher.
    /// </summary>
    public class PublisherAddress
    {
        /// <summary>
        /// Creates a new publisher based on the endpoint name.
        /// </summary>
        public PublisherAddress(EndpointName endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            this.endpoint = endpoint;
        }

        /// <summary>
        /// Creates a new publisher based on a set of endpoint instance names.
        /// </summary>
        public PublisherAddress(params EndpointInstance[] instances)
        {
            Guard.AgainstNull(nameof(instances), instances);
            if (instances.Length == 0)
            {
                throw new ArgumentException("You have to provide at least one instance.");
            }
            this.instances = instances;
        }

        /// <summary>
        /// Creates a new publisher based on a set of physical addresses.
        /// </summary>
        public PublisherAddress(params string[] addresses)
        {
            Guard.AgainstNull(nameof(addresses), addresses);
            if (addresses.Length == 0)
            {
                throw new ArgumentException("You need to provide at least one address.");
            }
            this.addresses = addresses;
        }

        internal async Task<IEnumerable<string>> Resolve(Func<EndpointName, Task<IEnumerable<EndpointInstance>>> instanceResolver, Func<EndpointInstance, string> addressResolver)
        {
            if (addresses != null)
            {
                return addresses;
            }
            if (instances != null)
            {
                return instances.Select(addressResolver);
            }
            var result = await instanceResolver(endpoint).ConfigureAwait(false);
            return result.Select(addressResolver);
        }

        string[] addresses;
        EndpointName endpoint;
        EndpointInstance[] instances;
    }
}