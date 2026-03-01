#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class InternallyManagedContainerHost(EndpointCreator endpointCreator, IServiceProvider serviceProvider) : IStartableEndpoint
{
    public Task<StartableEndpoint> Create(CancellationToken cancellationToken = default) =>
        startupRunner.Create(serviceProvider, cancellationToken);

    public Task<IEndpointInstance> Start(CancellationToken cancellationToken = default) =>
        startupRunner.Start(serviceProvider, cancellationToken);

    readonly EndpointStartupRunner startupRunner = new(endpointCreator.EndpointLogSlot, endpointCreator.CreateStartableEndpointForInternalContainer);
}