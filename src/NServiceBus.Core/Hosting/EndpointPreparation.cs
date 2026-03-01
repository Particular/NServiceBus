#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

static class EndpointPreparation
{
    public static async Task<StartableEndpoint> Prepare(
        object endpointLogSlot,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, StartableEndpoint> createStartableEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpointLogSlot);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(createStartableEndpoint);

        LoggingBridge.ResolveSlotFactory(serviceProvider, endpointLogSlot);

        using var _ = Logging.LogManager.BeginSlotScope(endpointLogSlot);
        var endpoint = createStartableEndpoint(serviceProvider);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return endpoint;
    }
}