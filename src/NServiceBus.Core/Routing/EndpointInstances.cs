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
        List<Func<EndpointName, IEnumerable<EndpointInstanceName>>> rules = new List<Func<EndpointName, IEnumerable<EndpointInstanceName>>>();

        internal IEnumerable<EndpointInstanceName> FindInstances(EndpointName endpoint)
        {
            var distinctInstances = rules.SelectMany(r => r(endpoint)).Distinct();
            return distinctInstances.EnsureNonEmpty(() => $"The list of instances of endpoint {endpoint} has not been provided to the routing module. Plase use 'BusConfiguration.Routing().EndpointInstances' to supply this information.");
        }


        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<EndpointName, IEnumerable<EndpointInstanceName>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void AddStatic(EndpointName endpoint, params EndpointInstanceName[] instances)
        {
            Guard.AgainstNull(nameof(endpoint), endpoint);
            if (instances.Length == 0)
            {
                throw new ArgumentException("The list of instances can't be empty.", nameof(instances));
            }
            if (instances.Any(i => i.EndpointName != endpoint))
            {
                throw new ArgumentException("At least one of the instances belongs to a different endpoint than specified in the 'endpoint' parameter.", nameof(instances));
            }
            rules.Add(e => StaticRule(e, endpoint, instances));   
        }

        static IEnumerable<EndpointInstanceName> StaticRule(EndpointName endpointBeingQueried, EndpointName configuredEndpoint, EndpointInstanceName[] configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return configuredInstances;
            }
            return Enumerable.Empty<EndpointInstanceName>();
        }
    }
}