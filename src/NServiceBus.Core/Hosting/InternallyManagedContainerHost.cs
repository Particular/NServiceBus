namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;

class InternallyManagedContainerHost : IStartableEndpoint
{
    public InternallyManagedContainerHost(StartableEndpoint endpoint)
    {
        this.endpoint = endpoint;
    }

    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return await endpoint.Start(cancellationToken).ConfigureAwait(false);
    }

    readonly StartableEndpoint endpoint;
}