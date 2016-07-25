namespace NServiceBus.Transport
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Routing;

    /// <summary>
    /// Manages the translation between endpoint instance names and physical addresses in direct routing.
    /// </summary>
    public class TransportAddresses
    {
        internal TransportAddresses(Func<EndpointInstance, string> instanceTransportDefault, Func<LogicalAddress, string> logicalTransportDefault)
        {
            this.instanceTransportDefault = instanceTransportDefault;
            this.logicalTransportDefault = logicalTransportDefault;
        }

        /// <summary>
        /// Adds an exception to the translation rules for a given endpoint instance.
        /// </summary>
        /// <param name="endpointInstance">Logical address for which the exception is created.</param>
        /// <param name="physicalAddress">Physical address of that instance.</param>
        public void AddSpecialCase(EndpointInstance endpointInstance, string physicalAddress)
        {
            Guard.AgainstNull(nameof(endpointInstance), endpointInstance);
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);

            exceptions[endpointInstance] = physicalAddress;
        }

        /// <summary>
        /// Adds a rule for translating endpoint instance names to physical addresses in direct routing.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddRule(Func<EndpointInstance, string> dynamicRule)
        {
            Guard.AgainstNull(nameof(dynamicRule), dynamicRule);
            rules.Add(dynamicRule);
        }

        internal string GetTransportAddress(LogicalAddress logicalAddress)
        {
            return logicalTransportDefault(logicalAddress);
        }

        internal string GetTransportAddress(EndpointInstance endpointInstance)
        {
            string exception;
            if (exceptions.TryGetValue(endpointInstance, out exception))
            {
                return exception;
            }
            var overrides = rules.Select(r => r(endpointInstance)).Where(a => a != null).ToArray();
            if (overrides.Length > 1)
            {
                throw new Exception("Translation of endpoint instance name " + endpointInstance + " to physical address using provided rules is ambiguous.");
            }
            return overrides.FirstOrDefault() ?? instanceTransportDefault(endpointInstance);
        }

        Dictionary<EndpointInstance, string> exceptions = new Dictionary<EndpointInstance, string>();
        List<Func<EndpointInstance, string>> rules = new List<Func<EndpointInstance, string>>();
        Func<EndpointInstance, string> instanceTransportDefault;
        Func<LogicalAddress, string> logicalTransportDefault;
    }
}