#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Transport;

class SatellitePipelineExecutor(IServiceProvider builder, SatelliteDefinition definition) : IPipelineExecutor
{
    public Task Invoke(MessageContext messageContext, CancellationToken cancellationToken = default)
    {
        messageContext.Extensions.Set(messageContext.TransportTransaction);

        return definition.OnMessage(builder, messageContext, cancellationToken);
    }
}