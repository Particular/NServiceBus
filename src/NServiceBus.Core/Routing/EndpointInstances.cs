namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stores the information about instances of known endpoints.
    /// </summary>
    public class EndpointInstances
    {
        List<Func<Endpoint, IEnumerable<EndpointInstance>>> rules = new List<Func<Endpoint, IEnumerable<EndpointInstance>>>();

        internal IEnumerable<EndpointInstance> FindInstances(Endpoint endpoint)
        {
            var distinctInstances = rules.SelectMany(r => r(endpoint)).Distinct().ToArray();
            return distinctInstances.EnsureNonEmpty(() => new EndpointInstance(endpoint, null, null));
        }


        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<Endpoint, IEnumerable<EndpointInstance>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void AddStatic(Endpoint endpoint, params EndpointInstance[] instances)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            if (instances.Length == 0)
            {
                throw new ArgumentException("The list of instances can't be empty.", nameof(instances));
            }
            if (instances.Any(i => i.Endpoint != endpoint))
            {
                throw new ArgumentException("At least one of the instances belongs to a different endpoint than specified in the 'endpoint' parameter.", nameof(instances));
            }
            rules.Add(e => StaticRule(e, endpoint, instances));   
        }

        static IEnumerable<EndpointInstance> StaticRule(Endpoint endpointBeingQueried, Endpoint configuredEndpoint, EndpointInstance[] configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return configuredInstances;
            }
            return Enumerable.Empty<EndpointInstance>();
        }
    }
}