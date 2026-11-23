#nullable enable

namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

sealed class EndpointStartupRunner(IEndpointCreationStrategy creationStrategy)
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

            startableEndpoint = await EndpointPreparation.Prepare(creationStrategy, serviceProvider, cancellationToken)
                .ConfigureAwait(false);
            return startableEndpoint;
        }
        finally
        {
            createSemaphore.Release();
        }
    }

    public async Task<RunningEndpointInstance> Start(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
    {
        var endpoint = await Create(serviceProvider, cancellationToken).ConfigureAwait(false);
        return await endpoint.Start(cancellationToken).ConfigureAwait(false);
    }

    readonly SemaphoreSlim createSemaphore = new(1, 1);
    volatile StartableEndpoint? startableEndpoint;
}