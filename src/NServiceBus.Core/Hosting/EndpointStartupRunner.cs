#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class EndpointStartupRunner(object endpointLogSlot, Func<IServiceProvider, StartableEndpoint> createStartableEndpoint)
{
    public async Task<StartableEndpoint> Create(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        if (startableEndpoint is not null)
        {
            return startableEndpoint;
        }

        await createSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (startableEndpoint is not null)
            {
                return startableEndpoint;
            }

            LoggingBridge.ResolveSlotFactory(serviceProvider, endpointLogSlot);

            var createdStartableEndpoint = createStartableEndpoint(serviceProvider);
            await createdStartableEndpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
            await createdStartableEndpoint.Setup(cancellationToken).ConfigureAwait(false);

            startableEndpoint = createdStartableEndpoint;
            return createdStartableEndpoint;
        }
        finally
        {
            createSemaphore.Release();
        }
    }

    readonly SemaphoreSlim createSemaphore = new(1, 1);
    StartableEndpoint? startableEndpoint;
}