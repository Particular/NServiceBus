namespace NServiceBus.Routing
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// Stores the information about instances of known endpoints.
    /// </summary>
    public class EndpointInstances
    {
        List<Func<EndpointName, Task<IEnumerable<EndpointInstance>>>> rules = new List<Func<EndpointName, Task<IEnumerable<EndpointInstance>>>>();

        internal async Task<IEnumerable<EndpointInstance>> FindInstances(EndpointName endpoint)
        {
            var instances = new List<EndpointInstance>();
            foreach (var rule in rules)
            {
                instances.AddRange(await rule.Invoke(endpoint).ConfigureAwait(false));
            }
            var distinctInstances = instances.Distinct().ToArray();
            return distinctInstances.EnsureNonEmpty(() => new EndpointInstance(endpoint));
        }


        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<EndpointName, Task<IEnumerable<EndpointInstance>>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void AddStatic(EndpointName endpoint, params EndpointInstance[] instances)
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

        static Task<IEnumerable<EndpointInstance>> StaticRule(EndpointName endpointBeingQueried, EndpointName configuredEndpoint, IEnumerable<EndpointInstance> configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return Task.FromResult(configuredInstances);
            }
            return Task.FromResult(Enumerable.Empty<EndpointInstance>());
        }
    }
}