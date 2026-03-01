#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

static class EndpointPreparation
{
    public static async Task<StartableEndpoint> Prepare(
        EndpointCreator endpointCreator,
        IServiceProvider serviceProvider,
        Func<IServiceProvider, StartableEndpoint> createStartableEndpoint,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(endpointCreator);
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(createStartableEndpoint);

        LoggingBridge.ResolveSlotFactory(serviceProvider, endpointCreator.EndpointLogSlot);

        using var _ = Logging.LogManager.BeginSlotScope(endpointCreator.EndpointLogSlot);
        var endpoint = createStartableEndpoint(serviceProvider);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return endpoint;
    }
}