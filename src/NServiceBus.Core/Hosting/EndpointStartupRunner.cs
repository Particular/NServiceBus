#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class EndpointStartupRunner(EndpointCreator endpointCreator, bool serviceProviderIsExternallyManaged)
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

            LoggingBridge.ResolveSlotFactory(serviceProvider, endpointCreator.EndpointLogSlot);

            var createdStartableEndpoint = endpointCreator.CreateStartableEndpoint(serviceProvider, serviceProviderIsExternallyManaged);
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