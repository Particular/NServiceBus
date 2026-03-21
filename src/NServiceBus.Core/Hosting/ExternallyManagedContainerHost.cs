#nullable enable
namespace NServiceBus;

using System;
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable CS0618 // Type or member is obsolete
class ExternallyManagedContainerHost(EndpointCreator endpointCreator) : IStartableEndpointWithExternallyManagedContainer
#pragma warning restore CS0618 // Type or member is obsolete
{
    public Lazy<IMessageSession> MessageSession { get; } = new(() => !endpointCreator.MessageSession.Initialized ? throw new InvalidOperationException("The message session can only be used after the endpoint is started.") : endpointCreator.MessageSession);

    public Task<StartableEndpoint> Create(IServiceProvider serviceProvider, CancellationToken cancellationToken = default)
        => startupRunner.Create(serviceProvider, cancellationToken);

    public Task<IEndpointInstance> Start(IServiceProvider externalBuilder, CancellationToken cancellationToken = default) =>
        startupRunner.Start(externalBuilder, cancellationToken);

    readonly EndpointStartupRunner startupRunner = new(new ExternalContainerEndpointCreationStrategy(endpointCreator));
}