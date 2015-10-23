namespace NServiceBus
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using NServiceBus.Extensibility;
    using NServiceBus.Transports;

    class OutboundTransports
    {
        List<OutboundTransport> transports = new List<OutboundTransport>();

        public void Add(TransportDefinition transportDefinition, ContextBag extensionContextBag, bool isDefault)
        {
            if (transports.Any(t => t.Definition.GetType() == transportDefinition.GetType()))
            {
                throw new InvalidOperationException($"Transport {transportDefinition.GetType().FullName} has been registered multiple times. Each transport can be used only once.");
            }
            transports.Add(new OutboundTransport(transportDefinition, extensionContextBag, isDefault));
        }

        public IReadOnlyCollection<OutboundTransport> Transports => transports;
    }
}