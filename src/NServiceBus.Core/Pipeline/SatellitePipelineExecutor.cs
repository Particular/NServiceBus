#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Transport;

class SatellitePipelineExecutor(IServiceProvider builder, SatelliteDefinition definition, object endpointLogSlot) : IPipelineExecutor
{
    public Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        using var _ = LogManager.BeginSlotScope(endpointLogSlot);

        messageContext.Extensions.Set(messageContext.TransportTransaction);

        return definition.OnMessage(builder, messageContext, cancellationToken);
    }
}