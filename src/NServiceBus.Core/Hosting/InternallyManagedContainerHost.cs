#nullable enable
namespace NServiceBus;

using System.Threading;
using System.Threading.Tasks;

class InternallyManagedContainerHost(StartableEndpoint endpoint) : IStartableEndpoint
{
    public async Task<IEndpointInstance> Start(CancellationToken cancellationToken = default)
    {
        await endpoint.Setup(cancellationToken).ConfigureAwait(false);
        return await endpoint.Start(cancellationToken).ConfigureAwait(false);
    }
}