#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

class ExternallyManagedContainerHost : IStartableEndpointWithExternallyManagedContainer
{
    public ExternallyManagedContainerHost(EndpointCreator endpointCreator)
    {
        this.endpointCreator = endpointCreator;

        MessageSession = new Lazy<IMessageSession>(() => !endpointCreator.MessageSession.Initialized ? throw new InvalidOperationException("The message session can only be used after the endpoint is started.") : endpointCreator.MessageSession);

        Builder = new Lazy<IServiceProvider>(() => objectBuilder ?? throw new InvalidOperationException("The builder can only be used after the endpoint is started."));
    }

    public Lazy<IMessageSession> MessageSession { get; }

    internal Lazy<IServiceProvider> Builder { get; private set; }

    public async Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken = default)
    {
        objectBuilder = externalBuilder;
        var startableEndpoint = endpointCreator.CreateStartableEndpoint(externalBuilder, serviceProviderIsExternallyManaged: true);
        await startableEndpoint.RunInstallers(cancellationToken).ConfigureAwait(false);
        await startableEndpoint.Setup(cancellationToken).ConfigureAwait(false);
        return await startableEndpoint.Start(cancellationToken).ConfigureAwait(false);
    }

    readonly EndpointCreator endpointCreator;
    IServiceProvider? objectBuilder;
}