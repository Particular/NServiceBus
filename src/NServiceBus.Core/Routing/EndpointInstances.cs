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
        List<Func<EndpointName, IEnumerable<EndpointInstanceData>>> rules = new List<Func<EndpointName, IEnumerable<EndpointInstanceData>>>();

        internal IEnumerable<EndpointInstanceData> FindInstances(EndpointName endpoint)
        {
            var distinctInstances = rules.SelectMany(r => r(endpoint)).Distinct();
            return distinctInstances.EnsureNonEmpty(() => $"The list of instances of endpoint {endpoint} has not been provided to the routing module. Plase use 'BusConfiguration.Routing().EndpointInstances' to supply this information.");
        }

        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<EndpointName, IEnumerable<EndpointInstanceData>> dynamicRule)
        {
            rules.Add(dynamicRule);
        } 
        
        /// <summary>
        /// Adds a dynamic rule for determining endpoint instances.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddDynamic(Func<EndpointName, IEnumerable<EndpointInstanceName>> dynamicRule)
        {
            rules.Add(e => dynamicRule(e).Select(i => new EndpointInstanceData(i)));
        }

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void AddStatic(EndpointName endpoint, params EndpointInstanceData[] instances)
        {
            rules.Add(e => StaticRule(e, endpoint, instances));   
        }
        
        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="instances">A static list of endpoint's instances.</param>
        public void AddStatic(EndpointName endpoint, params EndpointInstanceName[] instances)
        {
            rules.Add(e => StaticRule(e, endpoint, instances.Select(i => new EndpointInstanceData(i)).ToArray()));   
        }

        /// <summary>
        /// Adds static information about an endpoint.
        /// </summary>
        /// <param name="endpoint">Name of the endpoint.</param>
        /// <param name="transportDiscriminators">A static list of endpoint instances' transport discriminators.</param>
        public void AddStaticUsingTransportDiscriminators(EndpointName endpoint, params string[] transportDiscriminators)
        {
            AddStatic(endpoint, transportDiscriminators.Select(d => new EndpointInstanceName(endpoint, null, d)).ToArray());
        }

        private static IEnumerable<EndpointInstanceData> StaticRule(EndpointName endpointBeingQueried, EndpointName configuredEndpoint, EndpointInstanceData[] configuredInstances)
        {
            if (endpointBeingQueried == configuredEndpoint)
            {
                return configuredInstances;
            }
            return Enumerable.Empty<EndpointInstanceData>();
        }
    }
}