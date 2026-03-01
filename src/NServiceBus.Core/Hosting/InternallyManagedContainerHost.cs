#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class InternallyManagedContainerHost(EndpointCreator endpointCreator, IServiceProvider serviceProvider) : IStartableEndpoint
{
    public Task<StartableEndpoint> Create(CancellationToken cancellationToken = default) =>
        startupRunner.Create(serviceProvider, cancellationToken);

    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        var endpoint = await Create(cancellationToken).ConfigureAwait(false);
        return await endpoint.Start(cancellationToken).ConfigureAwait(false);
    }

    readonly EndpointStartupRunner startupRunner = new(endpointCreator.EndpointLogSlot, endpointCreator.CreateStartableEndpointForInternalContainer);
}