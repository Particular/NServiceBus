#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete
class InternallyManagedContainerHost(EndpointCreator endpointCreator, IServiceProvider serviceProvider) : IStartableEndpoint
#pragma warning restore CS0618 // Type or member is obsolete
{
    public Task<StartableEndpoint> Create(CancellationToken cancellationToken = default) =>
        startupRunner.Create(serviceProvider, cancellationToken);

    public Task<IEndpointInstance> Start(CancellationToken cancellationToken = default) =>
        startupRunner.Start(serviceProvider, cancellationToken);

    readonly EndpointStartupRunner startupRunner = new(new InternalContainerEndpointCreationStrategy(endpointCreator));
}