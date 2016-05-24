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
        internal async Task<IEnumerable<EndpointInstance>> FindInstances(EndpointName endpoint)
        {
            var instances = new HashSet<EndpointInstance>();
            foreach (var rule in rules)
            {
                var instancesFromRule = await rule.Invoke(endpoint).ConfigureAwait(false);
                foreach (var instance in instancesFromRule)
                {
                    instances.Add(instance);
                }
            }
            return instances.EnsureNonEmpty(() => new EndpointInstance(endpoint));
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
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void Add(params EndpointInstance[] instances) => Add((IEnumerable<EndpointInstance>)instances);

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void Add(IEnumerable<EndpointInstance> instances)
        {
            if (!instances.Any())
            {
                throw new ArgumentException("The list of instances can't be empty.", nameof(instances));
            }
            var endpointsByName = instances.GroupBy(i => i.Endpoint);
            foreach (var instanceGroup in endpointsByName)
            {
                rules.Add(e => StaticRule(e, instanceGroup.Key, instanceGroup));
            }
        }

        static Task<IEnumerable<EndpointInstance>> StaticRule(EndpointName endpointBeingQueried, EndpointName configuredEndpoint, IEnumerable<EndpointInstance> configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return Task.FromResult(configuredInstances);
            }
            return EmptyStaticRuleTask;
        }

        List<Func<EndpointName, Task<IEnumerable<EndpointInstance>>>> rules = new List<Func<EndpointName, Task<IEnumerable<EndpointInstance>>>>();
        static Task<IEnumerable<EndpointInstance>> EmptyStaticRuleTask = Task.FromResult(Enumerable.Empty<EndpointInstance>());
    }
}