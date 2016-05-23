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
        internal async Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
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
        public void AddDynamic(Func<string, Task<IEnumerable<EndpointInstance>>> dynamicRule)
        {
            rules.Add(dynamicRule);
        }

        /// <summary>
        /// Registers provided endpoint instances.
        /// </summary>
        /// <param name="instances">A static list of endpoint instances.</param>
        public void Add(params EndpointInstance[] instances) => Add((IEnumerable<EndpointInstance>)instances);

        /// <summary>
        /// Registers provided endpoint instances.
        /// </summary>
        /// <param name="instances">A static list of endpoint instances.</param>
        public void Add(IEnumerable<EndpointInstance> instances)
        {
            Guard.AgainstNull(nameof(instances), instances);
            var endpointsByName = instances.Select(i =>
            {
                if (i == null)
                {
                    throw new ArgumentNullException(nameof(instances), "One of the elements of collection is null");
                }
                return i;
            }).GroupBy(i => i.Endpoint);
            foreach (var instanceGroup in endpointsByName)
            {
                rules.Add(e => StaticRule(e, instanceGroup.Key, instanceGroup));
            }
        }

        static Task<IEnumerable<EndpointInstance>> StaticRule(string endpointBeingQueried, string configuredEndpoint, IEnumerable<EndpointInstance> configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return Task.FromResult(configuredInstances);
            }
            return EmptyStaticRuleTask;
        }

        List<Func<string, Task<IEnumerable<EndpointInstance>>>> rules = new List<Func<string, Task<IEnumerable<EndpointInstance>>>>();
        static Task<IEnumerable<EndpointInstance>> EmptyStaticRuleTask = Task.FromResult(Enumerable.Empty<EndpointInstance>());
    }
}