namespace NServiceBus.Routing
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Stores the information about instances of known endpoints.
    /// </summary>
    public class EndpointInstances
    {
        /// <summary>
        /// Returns all known <see cref="EndpointInstance"/> for a given logical endpoint.
        /// </summary>
        /// <param name="endpoint">The logical endpoint name.</param>
        /// <returns>Returns at least one <see cref="EndpointInstance"/>.</returns>
        public IEnumerable<EndpointInstance> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> registeredInstances;
            return allInstances.TryGetValue(endpoint, out registeredInstances)
                ? registeredInstances
                : DefaultInstance(endpoint);
        }

        static IEnumerable<EndpointInstance> DefaultInstance(string endpoint)
        {
            yield return new EndpointInstance(endpoint);
        }

        /// <summary>
        /// Adds or replaces a set of endpoint instances registered under a given key (registration source ID).
        /// </summary>
        /// <param name="sourceKey">Source key.</param>
        /// <param name="endpointInstances">List of endpoint instances known by this source.</param>
        public void AddOrReplaceInstances(string sourceKey, IList<EndpointInstance> endpointInstances)
        {
            Guard.AgainstNull(nameof(sourceKey), sourceKey);
            Guard.AgainstNull(nameof(endpointInstances), endpointInstances);
            lock (updateLock)
            {
                registrations[sourceKey] = endpointInstances;
                var newCache = new Dictionary<string, HashSet<EndpointInstance>>();

                foreach (var instance in registrations.Values.SelectMany(x => x))
                {
                    HashSet<EndpointInstance> instanceSet;
                    if (!newCache.TryGetValue(instance.Endpoint, out instanceSet))
                    {
                        instanceSet = new HashSet<EndpointInstance>();
                        newCache[instance.Endpoint] = instanceSet;
                    }
                    instanceSet.Add(instance);
                }
                allInstances = newCache;
            }
        }

        Dictionary<string, HashSet<EndpointInstance>> allInstances = new Dictionary<string, HashSet<EndpointInstance>>();
        Dictionary<object, IList<EndpointInstance>> registrations = new Dictionary<object, IList<EndpointInstance>>();
        object updateLock = new object();
    }
}