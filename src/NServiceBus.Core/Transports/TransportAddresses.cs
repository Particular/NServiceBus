namespace NServiceBus.Transports
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using JetBrains.Annotations;

    /// <summary>
    /// Manages the translation between endpoint instance names and physical addresses in direct routing.
    /// </summary>
    public class TransportAddresses
    {
        List<Func<EndpointInstanceName, string>> rules = new List<Func<EndpointInstanceName, string>>();
        Dictionary<EndpointInstanceName, string> exceptions = new Dictionary<EndpointInstanceName, string>();
        Func<EndpointInstanceName, string> transportDefault;

        /// <summary>
        /// Adds an exception to the translation rules for a given endpoint instance.
        /// </summary>
        /// <param name="endpointInstance">Name of the instance for which the exception is created.</param>
        /// <param name="physicalAddress">Physical address of that instance.</param>
        public void AddException([NotNull] EndpointInstanceName endpointInstance, string physicalAddress)
        {
            Guard.AgainstNull(nameof(endpointInstance),endpointInstance);
            Guard.AgainstNullAndEmpty(nameof(physicalAddress), physicalAddress);

            exceptions[endpointInstance] = physicalAddress;
        }

        /// <summary>
        /// Adds a rule for translating endpoint instance names to physical addresses in direct routing.
        /// </summary>
        /// <param name="dynamicRule">The rule.</param>
        public void AddRule(Func<EndpointInstanceName, string> dynamicRule)
        {
            Guard.AgainstNull(nameof(dynamicRule), dynamicRule);
            rules.Add(dynamicRule);
        }

        internal void RegisterTransportDefault(Func<EndpointInstanceName, string> transportDefault)
        {
            this.transportDefault = transportDefault;
        }

        internal string GetPhysicalAddress(EndpointInstanceName endpointInstance)
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
            return overrides.FirstOrDefault() ?? transportDefault(endpointInstance);
        }
    }
}