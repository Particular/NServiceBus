namespace NServiceBus.Transport
{
    using System;
    using Routing;

    /// <summary>
    /// Manages the translation between endpoint instance names and physical addresses in direct routing.
    /// </summary>
    public class TransportAddresses
    {
        internal TransportAddresses(Func<string, string> logicalNameToTransportAddress, Func<EndpointInstance, string> endpointInstanceToTransportAddress)
        {
            this.logicalNameToTransportAddress = logicalNameToTransportAddress;
            this.endpointInstanceToTransportAddress = endpointInstanceToTransportAddress;
        }

        internal string GetTransportAddress(string endpointInstance)
        {
            return logicalNameToTransportAddress(endpointInstance);
        }

        internal string GetTransportAddress(EndpointInstance endpointInstance)
        {
            return endpointInstanceToTransportAddress(endpointInstance);
        }

        Func<string, string> logicalNameToTransportAddress;
        readonly Func<EndpointInstance, string> endpointInstanceToTransportAddress;
    }
}