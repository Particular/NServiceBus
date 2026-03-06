#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

static class EndpointPreparation
{
    public static async Task<StartableEndpoint> Prepare(
        IEndpointCreationStrategy creationStrategy,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(creationStrategy);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        LoggingBridge.ResolveSlotFactory(serviceProvider, creationStrategy.EndpointLogSlot);

        using var _ = Logging.LogManager.BeginSlotScope(creationStrategy.EndpointLogSlot);
        var endpoint = creationStrategy.CreateStartableEndpoint(serviceProvider);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return endpoint;
    }
}