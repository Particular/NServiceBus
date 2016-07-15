namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// A destination of address routing.
    /// </summary>
    public class UnicastRoute : IUnicastRoute
    {
        /// <summary>
        /// Creates a destination based on the name of the endpoint.
        /// </summary>
        /// <param name="endpoint">Destination endpoint.</param>
        /// <returns>The new destination route.</returns>
        public static UnicastRoute CreateFromEndpointName(string endpoint)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            return new UnicastRoute { endpoint = endpoint };
        }

        ///// <summary>
        ///// Creates a destination based on the name of the endpoint instance.
        ///// </summary>
        ///// <param name="instance">Destination instance name.</param>
        ///// <returns>The new destination route.</returns>
        //public static UnicastRoute CreateFromEndpointInstance(EndpointInstance instance)
        //{
        //    Guard.AgainstNull(nameof(instance), instance);
        //    return new UnicastRoute { instance = instance };
        //}

        private UnicastRoute()
        {
        }

        async Task<IEnumerable<UnicastRoutingTarget>> IUnicastRoute.Resolve(Func<string, Task<IEnumerable<EndpointInstance>>> instanceResolver)
        {
            //if (instance != null)
            //{
            //    return EnumerableEx.Single(UnicastRoutingTarget.ToEndpointInstance(instance));
            //}
            var instances = await instanceResolver(endpoint).ConfigureAwait(false);
            return instances.Select(UnicastRoutingTarget.ToEndpointInstance);
        }

        string endpoint;
        //EndpointInstance instance;
    }
}