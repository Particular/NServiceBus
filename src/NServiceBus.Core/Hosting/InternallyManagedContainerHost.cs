#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete -- In the next major version this type can probably be removed including the associated strategy because there will be no internally managed mode anymore
class InternallyManagedContainerHost(EndpointCreator endpointCreator, IServiceProvider serviceProvider) : IStartableEndpoint
#pragma warning restore CS0618 // Type or member is obsolete
{
    public Task<StartableEndpoint> Create(CancellationToken cancellationToken = default) =>
        startupRunner.Create(serviceProvider, cancellationToken);

#pragma warning disable CS0618 // Type or member is obsolete
    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default) =>
        await startupRunner.Start(serviceProvider, cancellationToken).ConfigureAwait(false);
#pragma warning restore CS0618 // Type or member is obsolete

    readonly EndpointStartupRunner startupRunner = new(new InternalContainerEndpointCreationStrategy(endpointCreator));
}