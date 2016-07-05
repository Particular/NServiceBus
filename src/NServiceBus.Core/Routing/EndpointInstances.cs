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
        internal Task<IEnumerable<EndpointInstance>> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> staticInstances;
            staticRules.TryGetValue(endpoint, out staticInstances);

            if (dynamicRules.Count > 0)
            {
                return FindDynamicInstances(endpoint, staticInstances);
            }

            if (staticInstances != null)
            {
                return Task.FromResult<IEnumerable<EndpointInstance>>(staticInstances);
            }

            return Task.FromResult<IEnumerable<EndpointInstance>>(new[]
            {
                new EndpointInstance(endpoint)
            });
        }

        async Task<IEnumerable<EndpointInstance>> FindDynamicInstances(string endpoint, HashSet<EndpointInstance> staticInstances)
        {
            var dynamicInstances = staticInstances != null ? new HashSet<EndpointInstance>(staticInstances) : new HashSet<EndpointInstance>();
            foreach (var rule in dynamicRules)
            {
                var instancesFromRule = await rule.Invoke(endpoint).ConfigureAwait(false);
                foreach (var instance in instancesFromRule)
                {
                    dynamicInstances.Add(instance);
                }
            }

            if (!dynamicInstances.Any())
            {
                return new[]
                {
                    new EndpointInstance(endpoint)
                };
            }

            return dynamicInstances;
        }

        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<string, Task<IEnumerable<EndpointInstance>>> dynamicRule)
        {
            dynamicRules.Add(dynamicRule);
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

            foreach (var instance in instances)
            {
                Add(instance);
            }
        }

        /// <summary>
        /// Registers provided endpoint instance.
        /// </summary>
        /// <param name="instance">The endpoint instance.</param>
        public void Add(EndpointInstance instance)
        {
            Guard.AgainstNull(nameof(instance), instance);

            HashSet<EndpointInstance> existingInstances;
            if (staticRules.TryGetValue(instance.Endpoint, out existingInstances))
            {
                existingInstances.Add(instance);
            }
            else
            {
                staticRules.Add(instance.Endpoint, new HashSet<EndpointInstance>
                    {
                        instance
                    });
            }
        }

        Dictionary<string, HashSet<EndpointInstance>> staticRules = new Dictionary<string, HashSet<EndpointInstance>>();
        List<Func<string, Task<IEnumerable<EndpointInstance>>>> dynamicRules = new List<Func<string, Task<IEnumerable<EndpointInstance>>>>();
    }
}