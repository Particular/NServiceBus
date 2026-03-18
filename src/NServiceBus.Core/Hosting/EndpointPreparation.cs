#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;
using Logging;
using Microsoft.Extensions.DependencyInjection;
using MicrosoftLoggerFactory = Microsoft.Extensions.Logging.ILoggerFactory;

static class EndpointPreparation
{
    public static async Task<StartableEndpoint> Prepare(
        IEndpointCreationStrategy creationStrategy,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(creationStrategy);
        ArgumentNullException.ThrowIfNull(serviceProvider);

        var slotLoggerFactory = LogManager.TryGetExternalFactory()
            ?? new MicrosoftLoggerFactoryAdapter(serviceProvider.GetRequiredService<MicrosoftLoggerFactory>());
        LogManager.RegisterSlotFactory(creationStrategy.EndpointLogSlot, slotLoggerFactory);

        using var _ = LogManager.BeginSlotScope(creationStrategy.EndpointLogSlot);
        var endpoint = creationStrategy.CreateStartableEndpoint(serviceProvider);
        await endpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return endpoint;
    }
}