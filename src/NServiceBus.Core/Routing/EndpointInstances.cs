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
        internal IEnumerable<EndpointInstance> FindInstances(string endpoint)
        {
            HashSet<EndpointInstance> instances;
            return cache.TryGetValue(endpoint, out instances)
                ? instances
                : EnumerableEx.Single(new EndpointInstance(endpoint));
        }

        internal void SetLogChangeAction(Action<string> logChangeAction)
        {
            this.logChangeAction = logChangeAction;
        }

        /// <summary>
        /// Adds or replaces a set of endpoint instances registered under a given key (registration source ID).
        /// </summary>
        /// <param name="sourceKey">Source key.</param>
        /// <param name="endpointInstances">List of endpoint instances known by this source.</param>
        public void AddOrReplaceInstances(object sourceKey, IList<EndpointInstance> endpointInstances)
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
                cache = newCache;
                if (logChangeAction != null)
                {
                    var routesFormatted = string.Join(Environment.NewLine, cache.Select(kvp => $"{kvp.Key} -> {FormatInstances(kvp.Value)}"));
                    logChangeAction($"Instance mapping refreshed.{Environment.NewLine}{routesFormatted}");
                }
            }
        }

        static string FormatInstances(IEnumerable<EndpointInstance> endpointInstances)
        {
            return string.Join(", ", endpointInstances.Select(x => $"[{x.ToString()}]").OrderBy(x => x));
        }

        Dictionary<string, HashSet<EndpointInstance>> cache = new Dictionary<string, HashSet<EndpointInstance>>();
        Dictionary<object, IList<EndpointInstance>> registrations = new Dictionary<object, IList<EndpointInstance>>();
        object updateLock = new object();
        Action<string> logChangeAction;
    }
}