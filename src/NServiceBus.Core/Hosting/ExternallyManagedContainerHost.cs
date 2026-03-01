#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class ExternallyManagedContainerHost(EndpointCreator endpointCreator) : IStartableEndpointWithExternallyManagedContainer
{
    public Lazy<IMessageSession> MessageSession { get; } = new(() => !endpointCreator.MessageSession.Initialized ? throw new InvalidOperationException("The message session can only be used after the endpoint is started.") : endpointCreator.MessageSession);

    public Task<StartableEndpoint> Create(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        => startupRunner.Create(serviceProvider, cancellationToken);

    public async Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken = default)
    {
        var endpoint = await Create(externalBuilder, cancellationToken).ConfigureAwait(false);
        return await endpoint.Start(cancellationToken).ConfigureAwait(false);
    }

    readonly EndpointStartupRunner startupRunner = new(endpointCreator.EndpointLogSlot, endpointCreator.CreateStartableEndpointForExternalContainer);
}